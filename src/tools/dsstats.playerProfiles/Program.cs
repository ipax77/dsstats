using dsstats.db;
using dsstats.dbServices;
using dsstats.dbServices.Stats;
using dsstats.shared;
using dsstats.shared.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;

// Read-only benchmark and local preview: no API background workers or migrations.
var builder = WebApplication.CreateBuilder(args);
var configPath = builder.Configuration["config"]
    ?? throw new ArgumentException("Pass --config=<local API configuration JSON>.");
builder.Configuration.AddJsonFile(Path.GetFullPath(configPath), optional: false);
builder.Logging.ClearProviders();
var capture = new ReadOnlyCommands();
builder.Services.AddSingleton(capture);
var connectionString = builder.Configuration["dsstats:ConnectionString"]
    ?? throw new InvalidOperationException("Missing dsstats:ConnectionString.");
if (!connectionString.Contains("SslMode=", StringComparison.OrdinalIgnoreCase))
    connectionString += ";SslMode=None";
builder.Services.AddDbContextFactory<DsstatsContext>(options => options.UseMySql(
    connectionString,
    new MySqlServerVersion(Version.Parse(builder.Configuration["dsstats:ServerVersion"] ?? "8.4.0")))
    .AddInterceptors(capture));
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IImportService>(provider =>
{
    var proxy = DispatchProxy.Create<IImportService, ReadOnlyPlayerLookup>();
    ((ReadOnlyPlayerLookup)(object)proxy).Factory = provider.GetRequiredService<IDbContextFactory<DsstatsContext>>();
    return proxy;
});
builder.Services.AddScoped<PlayerService>();
builder.Services.AddScoped<IPlayerService>(p => p.GetRequiredService<PlayerService>());
builder.Services.AddScoped<IPlayerProfileService>(p => p.GetRequiredService<PlayerService>());
builder.Services.AddScoped<IReplayRepository, ReplayRepository>();
builder.Services.AddStats();
var app = builder.Build();

if (builder.Configuration.GetValue<bool>("preview"))
{
    app.MapPost("/api11/Players/overview", async (PlayerProfileRequest r, IPlayerProfileService s, CancellationToken t) =>
    {
        var result = await s.GetOverview(r, t);
        return result is null ? Results.NotFound() : Results.Ok(result);
    });
    app.MapPost("/api11/Players/details", async (PlayerProfileRequest r, IPlayerProfileService s, CancellationToken t) =>
    {
        var result = await s.GetDetails(r, t);
        return result is null ? Results.NotFound() : Results.Ok(result);
    });
    app.MapPost("/api10/Players/ratings", (PlayerRatingsRequest r, IPlayerService s, CancellationToken t) => s.GetRatings(r, t));
    app.MapPost("/api10/Players/ratingscount", (PlayerRatingsRequest r, IPlayerService s, CancellationToken t) => s.GetRatingsCount(r, t));
    app.MapPost("/api10/Players/cmdrperf", (PlayerStatsRequest r, IPlayerService s, CancellationToken t) => s.GetCommandersPerformance(r, t));
    app.MapPost("/api10/Stats/user", async (UserStatsRequest r, IEnumerable<IStatsProvider> providers, CancellationToken t) =>
        Results.Json(await providers.Single(p => p.StatsType == r.Request.Type).GetUserStatsUntypedAsync(r.Request, r.ToonId, t)));
    app.MapGet("/api10/Replays/{hash}", (string hash, IReplayRepository s) => s.GetReplayDetails(hash));
    await app.RunAsync("http://localhost:5289");
    return;
}

await using var scope = app.Services.CreateAsyncScope();
var service = scope.ServiceProvider.GetRequiredService<PlayerService>();
var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<DsstatsContext>>();
await using var db = await factory.CreateDbContextAsync();
var candidates = await db.PlayerRatings.AsNoTracking()
    .Where(r => r.RatingType == RatingType.Commanders && r.DsstatsGames >= 50)
    .Select(r => new { r.PlayerId, r.DsstatsGames, r.Player!.Name, r.Player.ToonId })
    .OrderBy(r => r.DsstatsGames).ToListAsync();
if (candidates.Count == 0) throw new InvalidOperationException("No rated candidates in the local dump.");
var selected = new[] { candidates[0], candidates[candidates.Count / 2], candidates[^1] };
// Warm the metadata lookup equally for all measured flows.
scope.ServiceProvider.GetRequiredService<IImportService>().GetPlayerId(new()
{
    Id = selected[0].ToonId.Id, Region = selected[0].ToonId.Region, Realm = selected[0].ToonId.Realm
});
var results = new List<object>();
var plans = new List<object>();
foreach (var player in selected)
{
    var request = new PlayerProfileRequest
    {
        ToonId = new() { Id = player.ToonId.Id, Region = player.ToonId.Region, Realm = player.ToonId.Realm },
        RatingType = RatingType.Commanders
    };
    var legacy = new PlayerStatsRequest { ToonId = request.ToonId, RatingType = request.RatingType };
    foreach (var flow in new[] { "legacy", "overview", "details", "overview+details" })
    {
        for (int run = 0; run < 4; run++)
        {
            capture.Commands.Clear();
            long allocated = GC.GetTotalAllocatedBytes(precise: true);
            var watch = Stopwatch.StartNew();
            object? result = flow switch
            {
                "legacy" => await service.GetPlayerStats(legacy),
                "overview" => await service.GetOverview(request),
                "details" => await service.GetDetails(request),
                _ => new { Overview = await service.GetOverview(request), Details = await service.GetDetails(request) }
            };
            watch.Stop();
            allocated = GC.GetTotalAllocatedBytes(precise: true) - allocated;
            results.Add(new
            {
                player.PlayerId, player.Name, player.DsstatsGames, ToonId = request.ToonId,
                Flow = flow, Run = run, ElapsedMs = Math.Round(watch.Elapsed.TotalMilliseconds, 2),
                AllocatedBytes = allocated, SqlCommands = capture.Commands.Count,
                ResponseBytes = JsonSerializer.SerializeToUtf8Bytes(result).Length
            });
            if (player == selected[^1] && run == 3 && flow is "overview" or "details")
            {
                var commands = capture.Commands.ToArray();
                await db.Database.OpenConnectionAsync();
                foreach (var command in commands)
                {
                    await using var explain = db.Database.GetDbConnection().CreateCommand();
                    explain.CommandText = "EXPLAIN FORMAT=JSON " + command.Sql;
                    foreach (var parameter in command.Parameters)
                    {
                        var p = explain.CreateParameter();
                        p.ParameterName = parameter.Key;
                        p.Value = parameter.Value ?? DBNull.Value;
                        explain.Parameters.Add(p);
                    }
                    plans.Add(new { Flow = flow, command.Sql, Plan = await explain.ExecuteScalarAsync() });
                }
            }
        }
    }
}
var report = new
{
    CreatedUtc = DateTime.UtcNow,
    Notes = "Read-only local dump. Run 0 is first invocation, not a cold database. Runs 1–3 are repeated. Metadata lookup is warmed; database and percentile caches are shared. Allocations are process-wide. No HTTP or chart timings.",
    Results = results, Plans = plans
};
var output = builder.Configuration["output"] ?? "player-profile-benchmark.json";
await File.WriteAllTextAsync(output, JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine($"Wrote {results.Count} measurements and {plans.Count} query plans to {Path.GetFullPath(output)}");

public class ReadOnlyPlayerLookup : DispatchProxy
{
    public IDbContextFactory<DsstatsContext> Factory { get; set; } = null!;
    private Dictionary<(int, int, int), (int Id, string Name)>? players;
    protected override object? Invoke(MethodInfo? method, object?[]? args)
    {
        if (method?.Name is not ("GetPlayerId" or "GetPlayerName")) throw new NotSupportedException("Read-only preview.");
        if (players is null)
        {
            using var db = Factory.CreateDbContext();
            players = db.Players.AsNoTracking().Select(p => new { p.PlayerId, p.Name, p.ToonId })
                .ToList().ToDictionary(p => (p.ToonId.Region, p.ToonId.Realm, p.ToonId.Id), p => (p.PlayerId, p.Name));
        }
        var toon = (ToonIdDto)args![0]!;
        var player = players.GetValueOrDefault((toon.Region, toon.Realm, toon.Id));
        return method.Name == "GetPlayerId" ? player.Id : player.Name ?? string.Empty;
    }
}

public sealed class ReadOnlyCommands : DbCommandInterceptor
{
    public List<(string Sql, Dictionary<string, object?> Parameters)> Commands { get; } = [];
    private void Capture(DbCommand command)
    {
        var sql = command.CommandText.TrimStart();
        while (sql.StartsWith("--")) sql = sql[(sql.IndexOf('\n') + 1)..].TrimStart();
        if (!sql.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The benchmark permits SELECT statements only.");
        Commands.Add((command.CommandText, command.Parameters.Cast<DbParameter>()
            .ToDictionary(p => p.ParameterName, p => (object?)p.Value)));
    }
    public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
    { Capture(command); return result; }
    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData,
        InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
    { Capture(command); return ValueTask.FromResult(result); }
    public override InterceptionResult<int> NonQueryExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<int> result) =>
        throw new InvalidOperationException("Read-only benchmark.");
    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData,
        InterceptionResult<int> result, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("Read-only benchmark.");
}
