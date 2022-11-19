using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using dsstats.raven;
using pax.dsstats.dbng;
using Microsoft.EntityFrameworkCore;
using pax.dsstats.shared;
using Raven.Client.Documents.Session;
using System.Text;
using System.Globalization;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using dsstats.mmr;

namespace dsstats.mmrconsole;

internal class Program
{
    static void Main(string[] args)
    {
        var json = JsonSerializer.Deserialize<JsonElement>(File.ReadAllText("/data/localserverconfig.json"));
        var config = json.GetProperty("ServerConfig");
        var connectionString = config.GetProperty("DsstatsConnectionString").GetString();
        var serverVersion = new MySqlServerVersion(new System.Version(5, 0, 40));

        var services = new ServiceCollection();

        services.AddAutoMapper(typeof(AutoMapperProfile));
        services.AddSingleton<DocumentStoreHolder>();
        services.AddScoped<IRatingRepository, RatingRepository>();
        services.AddLogging(builder =>
            {
                builder.ClearProviders();
                // Clear Microsoft's default providers (like eventlogs and others)
                builder.AddSimpleConsole(options =>
                {
                    options.IncludeScopes = true;
                    options.SingleLine = true;
                    options.TimestampFormat = "yyyy-MM-dd hh:mm:ss ";
                }).SetMinimumLevel(LogLevel.Warning);
            });
        services.AddDbContext<ReplayContext>(options =>
        {
            options.UseMySql(connectionString, serverVersion, p =>
            {
                p.EnableRetryOnFailure();
                p.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
            })
            // .EnableDetailedErrors()
            // .EnableSensitiveDataLogging()
            ;
        });

        var serviceProvider = services.BuildServiceProvider();

        using var scope = serviceProvider.CreateScope();
        var ratingRepository = scope.ServiceProvider.GetRequiredService<IRatingRepository>();

        // END SERVICE CONFIG

        Stopwatch sw = Stopwatch.StartNew();

        Produce(serviceProvider, false);

        sw.Stop();
        Console.WriteLine($"jobs done in {sw.ElapsedMilliseconds} ms");
    }

    internal static void Request()
    {
        RatingsRequest ratingsRequest = new()
        {
            Skip = 20,
            Take = 40,
            // Search = "Feralan",
            Orders = new()
            {
                new()
                {
                    Property = "Games",
                    Ascending = true
                },

            }
        };

        // var ratings = RatingRepository.GetPlayerRatings(ratingsRequest).GetAwaiter().GetResult();
    }

    internal static void CollectInital(int take)
    {
        using var session = DocumentStoreHolder.Store.OpenSession();

        var ratings = session.Query<PlayerRating>()
            .Statistics(out QueryStatistics stats)
            .OrderByDescending(o => o.Mmr)
            .Take(take)
            .ToList();

        Console.WriteLine($"got init data ({ratings.Count}|{stats.TotalResults}) in {stats.DurationInMs} ms");
    }

    internal static void Collect(int skip, int take)
    {
        using var session = DocumentStoreHolder.Store.OpenSession();

        Stopwatch sw = Stopwatch.StartNew();

        var ratings = session.Query<PlayerRating>()
            .OrderByDescending(o => o.Mmr)
            .Skip(skip)
            .Take(take)
            .ToList();

        sw.Stop();

        Console.WriteLine($"got data ({ratings.Count}) in {sw.ElapsedMilliseconds} ms");
    }

    internal static void Produce(IServiceProvider serviceProvider, bool mysql = false)
    {

        Stopwatch sw = Stopwatch.StartNew();

        // RavenService.DeleteRatings().GetAwaiter().GetResult();
        // sw.Stop();
        // Console.WriteLine($"cleared data in {sw.ElapsedMilliseconds} ms");

        //sw.Start();

        var data = GetCmdrReplayDsRDtos(serviceProvider, DateTime.MinValue, DateTime.MinValue)
            .GetAwaiter().GetResult();
        sw.Stop();
        Console.WriteLine($"got data in {sw.ElapsedMilliseconds} ms");

        sw.Restart();
        (var ratingResult, var changeResult) = MmrService.GeneratePlayerRatings(data);
        sw.Stop();

        Console.WriteLine($"calculated data in {sw.ElapsedMilliseconds} ms");

        sw.Restart();
        if (mysql)
        {
            SaveReplayPlayersData(serviceProvider, changeResult).GetAwaiter().GetResult();
            SavePlayersData(serviceProvider, ratingResult.Values.ToList()).GetAwaiter().GetResult();
        }
        else
        {
            using var scope = serviceProvider.CreateScope();
            var ratingRepository = scope.ServiceProvider.GetRequiredService<IRatingRepository>();
            // RavenService.BulkInsert(ratingResult.Values.ToList()).GetAwaiter().GetResult();
            // RavenService.BulkInsert(changeResult).GetAwaiter().GetResult();
            var result = ratingRepository.UpdatePlayerRatings(ratingResult.Values.ToList()).GetAwaiter().GetResult();
            var updateResult = ratingRepository.UpdateReplayPlayerMmrChanges(changeResult).GetAwaiter().GetResult();
            Console.WriteLine(result);
            Console.WriteLine(updateResult);
        }
        sw.Stop();

        Console.WriteLine($"data stored in {sw.ElapsedMilliseconds} ms");
    }

    private static async Task SaveReplayPlayersData(IServiceProvider serviceProvider, List<ReplayPlayerMmrChange> replayPlayerMmrChanges)
    {
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        StringBuilder sb = new();
        int i = 0;
        foreach (var replayPlayerMmrChange in replayPlayerMmrChanges)
        {
            sb.Append($"UPDATE {nameof(ReplayContext.ReplayPlayers)}" +
                $" SET {nameof(ReplayPlayer.MmrChange)} = {replayPlayerMmrChange.MmrChange.ToString(CultureInfo.InvariantCulture)}" +
                $" WHERE {nameof(ReplayPlayer.ReplayPlayerId)} = {replayPlayerMmrChange.ReplayPlayerId}; ");
            i++;
            if (i % 1000 == 0)
            {
                await context.Database.ExecuteSqlRawAsync(sb.ToString());
                sb.Clear();
            }
        }

        if (sb.Length > 0)
        {
            await context.Database.ExecuteSqlRawAsync(sb.ToString());
        }
    }

    private static async Task SavePlayersData(IServiceProvider serviceProvider, List<PlayerRating> playerRatings)
    {
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        StringBuilder sb = new();
        int i = 0;
        foreach (var playerRating in playerRatings)
        {
            sb.Append($"UPDATE {nameof(ReplayContext.Players)}" +
                $" SET {nameof(Player.Mmr)} = {playerRating.Mmr.ToString(CultureInfo.InvariantCulture)}, {nameof(Player.MmrOverTime)} = ''" +
                $" WHERE {nameof(Player.PlayerId)} = {playerRating.PlayerId}; ");

            i++;
            if (i % 500 == 0)
            {
                await context.Database.ExecuteSqlRawAsync(sb.ToString());
                sb.Clear();
            }
        }


        if (sb.Length > 0)
        {
            await context.Database.ExecuteSqlRawAsync(sb.ToString());
        }
    }

    public static async Task<List<ReplayDsRDto>> GetCmdrReplayDsRDtos(IServiceProvider serviceProvider, DateTime startTime, DateTime endTime)
    {
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

        var replays = context.Replays
            .Include(r => r.ReplayPlayers)
                .ThenInclude(rp => rp.Player)
            .Where(r => r.Playercount == 6
                && r.Duration >= 300
                && r.WinnerTeam > 0
                && (r.GameMode == GameMode.Commanders || r.GameMode == GameMode.CommandersHeroic))
            .AsNoTracking();

        if (startTime != DateTime.MinValue)
        {
            replays = replays.Where(x => x.GameTime >= startTime);
        }

        if (endTime != DateTime.MinValue && endTime < DateTime.Today)
        {
            replays = replays.Where(x => x.GameTime < endTime);
        }

        return await replays
            .OrderBy(o => o.GameTime)
                .ThenBy(o => o.ReplayId)
            .ProjectTo<ReplayDsRDto>(mapper.ConfigurationProvider)
            .ToListAsync();
    }
}


