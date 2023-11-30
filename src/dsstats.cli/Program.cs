using dsstats.db8;
using dsstats.db8.Extensions;
using dsstats.db8.AutoMapper;
using dsstats.ratings.lib;
using dsstats.db8services;
using dsstats.shared;
using dsstats.shared.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;
using AutoMapper;
using LinqKit;
using System.Security.Cryptography;
using System.Net.Mail;

namespace dsstats.cli;

class Program
{
    static void Main(string[] args)
    {
        var services = new ServiceCollection();

        var serverVersion = new MySqlServerVersion(new Version(5, 7, 42));
        var jsonStrg = File.ReadAllText("/data/localserverconfig.json");
        var json = JsonSerializer.Deserialize<JsonElement>(jsonStrg);
        var config = json.GetProperty("ServerConfig");
        var connectionString = config.GetProperty("DsstatsConnectionString").GetString();
        var importConnectionString = config.GetProperty("ImportConnectionString").GetString() ?? "";

        services.AddOptions<DbImportOptions>()
            .Configure(x =>
                {
                    x.ImportConnectionString = importConnectionString;
                    x.IsSqlite = false;
                });

        services.AddLogging(options =>
        {
            options.SetMinimumLevel(LogLevel.Information);
            options.AddFilter("System.Net.Http.HttpClient", LogLevel.Warning);
            options.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
            options.AddConsole();
        });

        services.AddDbContext<ReplayContext>(options =>
        {
            options.UseMySql(connectionString, serverVersion, p =>
            {
                p.CommandTimeout(600);
                p.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
                p.EnablePrimitiveCollectionsSupport();
            });
        });

        services.AddMemoryCache();
        services.AddAutoMapper(typeof(AutoMapperProfile));

        services.AddScoped<IWinrateService, WinrateService>();
        services.AddScoped<IReplaysService, ReplaysService>();
        // services.AddScoped<IDsstatsService, DsstatsService>();
        // services.AddScoped<IArcadeService, ArcadeService>();
        services.AddScoped<IPlayerService, PlayerService>();
        services.AddScoped<IBuildService, BuildService>();
        services.AddScoped<ICmdrInfoService, CmdrInfoService>();

        var serviceProvider = services.BuildServiceProvider();

        Stopwatch sw = Stopwatch.StartNew();

        // TestQuery(serviceProvider);
        TestReplayHashV2(serviceProvider);

        sw.Stop();
        Console.WriteLine($"job done in {sw.ElapsedMilliseconds} ms.");

        Console.ReadLine();
    }

    public static void TestReplayHashV2(ServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        Dictionary<string, DateTime> replayHashes = new();
        MD5 md5hash = MD5.Create();
        int skip = 0;
        int take = 1000;

        var replays = context.Replays
            .Include(i => i.ReplayPlayers)
                .ThenInclude(i => i.Player)
            .AsNoTracking()
            .OrderByDescending(o => o.GameTime)
                .ThenBy(o => o.ReplayId)
            .Take(take)
            .ToList();

        while (replays.Count > 0)
        {

            foreach (var replay in replays)
            {
                replay.GenHashV2(md5hash);
                if (!replayHashes.TryAdd(replay.ReplayHash, replay.GameTime))
                {
                    logger.LogWarning("failed adding replay hash {replayHash}, {gameTime} <=> {hashGameTime}",
                        replay.ReplayHash, replay.GameTime, replayHashes[replay.ReplayHash]);
                }
            }

            skip += take;
            replays = context.Replays
            .Include(i => i.ReplayPlayers)
                .ThenInclude(i => i.Player)
            .AsNoTracking()
            .OrderByDescending(o => o.GameTime)
                .ThenBy(o => o.ReplayId)
            .Skip(skip)
            .Take(take)
            .ToList();
        }
    }

    private static void TestQuery(ServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        List<PlayerId> playerIds = new()
        {
            new(10188255, 1, 1),
            new(226401, 1, 2)
        };

        var aPlayerIds = playerIds.Select(s => new { s.ToonId, s.RealmId, s.RegionId });

        var query = from r in context.Replays
                    from rp in r.ReplayPlayers
                    join rr in context.ReplayRatings on r.ReplayId equals rr.ReplayId
                    join rpr in context.RepPlayerRatings on rp.ReplayPlayerId equals rpr.ReplayPlayerId
                    where r.GameTime > new DateTime(2023, 1, 22)
                        && rr.RatingType == RatingType.Cmdr
                        && aPlayerIds.Contains(new { rp.Player.ToonId, rp.Player.RealmId, rp.Player.RegionId })
                    select r;

        var list = query
            .ToList();
    }

    private static void TestQuery2(ServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        List<PlayerId> playerIds = new()
        {
            new(10188255, 1, 1),
            new(226401, 1, 2)
        };


        var query = from r in context.Replays
                    from rp in r.ReplayPlayers
                    where r.GameTime > new DateTime(2023, 1, 22)
                    select rp;

        var predicate = PredicateBuilder.New<ReplayPlayer>(true);

        foreach (var playerId in playerIds)
        {
            // predicate = predicate.Or(o => playerIds.Contains())
        }
    }

    private static async Task<bool> CreateRatings(RatingCalcType ratingCalcType, ServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
        // var calcService = scope.ServiceProvider.GetRequiredService<CalcService>();

        // logger.LogInformation("Producing {type} ratings", ratingCalcType.ToString());
        // await calcService.GenerateRatings(ratingCalcType, recalc: true);

        return true;
    }

    public static async Task CreateAdjustTestData(ServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

        var replays1 = await context.Replays
            .Include(i => i.ReplayPlayers)
                .ThenInclude(i => i.Upgrades)
                    .ThenInclude(i => i.Upgrade)
            .Where(x =>
                x.GameTime > new DateTime(2023, 1, 1)
                && x.ResultCorrected
                && x.WinnerTeam == 1)
            .OrderByDescending(o => o.ReplayId)
            .Take(5)
            .AsNoTracking()
            .ToListAsync();

        var replays2 = await context.Replays
            .Include(i => i.ReplayPlayers)
                .ThenInclude(i => i.Upgrades)
                    .ThenInclude(i => i.Upgrade)
            .Where(x =>
                x.GameTime > new DateTime(2023, 1, 1)
                && x.ResultCorrected
                && x.WinnerTeam == 2)
            .OrderByDescending(o => o.ReplayId)
            .Take(5)
            .AsNoTracking()
            .ToListAsync();

        for (int i = 0; i < replays1.Count; i++)
        {
            var replay = replays1[i];
            ResetReplayWinner(replay);
            var replayDto = mapper.Map<ReplayDto>(replay);
            var json = JsonSerializer.Serialize(replayDto);
            File.WriteAllText($"/data/ds/adjustTestTeam1_{i}.json", json);
        }

        for (int i = 0; i < replays2.Count; i++)
        {
            var replay = replays2[i];
            ResetReplayWinner(replay);
            var replayDto = mapper.Map<ReplayDto>(replay);
            var json = JsonSerializer.Serialize(replayDto);
            File.WriteAllText($"/data/ds/adjustTestTeam2_{i}.json", json);
        }
    }

    private static void ResetReplayWinner(Replay replay)
    {
        replay.WinnerTeam = 0;
        foreach (var rp in replay.ReplayPlayers)
        {
            rp.PlayerResult = PlayerResult.None;
        }
    }
}
