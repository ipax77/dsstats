using dsstats.db8;
using dsstats.db8.AutoMapper;
using dsstats.db8.Extensions;
using dsstats.shared;
using dsstats.shared.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;

namespace dsstats.ratings;

class Program
{
    static void Main(string[] args)
    {
        var services = new ServiceCollection();

        var jsonStrg = File.ReadAllText("/data/localserverconfig.json");
        var json = JsonSerializer.Deserialize<JsonElement>(jsonStrg);
        var config = json.GetProperty("ServerConfig");
        var importConnectionString = config.GetProperty("ImportConnectionString").GetString() ?? "";
        var mySqlConnectionString = config.GetProperty("DsstatsConnectionString").GetString();
        // var mySqlConnectionString = config.GetProperty("ProdConnectionString").GetString();

        services.AddOptions<DbImportOptions>()
            .Configure(x =>
                {
                    x.ImportConnectionString = importConnectionString;
                    x.IsSqlite = false;
                });

        services.AddLogging(options =>
        {
            options.SetMinimumLevel(LogLevel.Information);
            options.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
            options.AddConsole();
        });

        services.AddDbContext<ReplayContext>(options =>
        {
            options.UseMySql(mySqlConnectionString, ServerVersion.AutoDetect(mySqlConnectionString), p =>
            {
                p.CommandTimeout(600);
                p.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
            });
        });

        services.AddAutoMapper(typeof(AutoMapperProfile));
        services.AddScoped<ComboRatings>();
        services.AddSingleton<IRatingService, RatingService>();
        services.AddSingleton<IRatingsSaveService, RatingsSaveService>();

        var serviceProvider = services.BuildServiceProvider();

        var scope = serviceProvider.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        logger.LogInformation("ratings start.");

        if (args.Length == 0)
        {
            args = ["combo"];
        }

        Stopwatch sw = Stopwatch.StartNew();
        if (args.Length > 0)
        {
            bool recalc = true;
            if (args.Length > 1 && args[1] == "continue")
            {
                recalc = false;
            }

            var ratingService = scope.ServiceProvider.GetRequiredService<IRatingService>();
            if (args[0] == "dsstats")
            {
                logger.LogInformation("producing dsstats ratings.");
                ratingService.ProduceRatings(RatingCalcType.Dsstats, recalc).Wait();
            }
            else if (args[0] == "arcade")
            {
                logger.LogInformation("producing sc2arcade ratings.");
                ratingService.ProduceRatings(RatingCalcType.Arcade, recalc).Wait();
            }
            else if (args[0] == "combo")
            {
                logger.LogInformation("producing combo ratings.");
                ratingService.ProduceRatings(RatingCalcType.Combo, recalc).Wait();
            }
            else if (args[0] == "lsdups")
            {
                logger.LogInformation("Checking lastSpawnHashes");
                SetLastSpawnHashes(serviceProvider);
                CheckLastSpawnHashDuplicates(serviceProvider);
            }
            else if (args[0] == "prep")
            {
                logger.LogInformation("CombineDsstatsSc2ArcadeReplays");
                var comboRatings = scope.ServiceProvider.GetRequiredService<ComboRatings>();
                comboRatings.CombineDsstatsSc2ArcadeReplays(add: false).Wait();
            }
            else
            {
                logger.LogError("allowed parameters: dsstats|sc2arcade|combo");
                return;
            }
        }
        else if (args.Length == 2)
        {
            if (args[0] == "delete")
            {
                var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
                DeleteReplay(args[1], context, logger);
            }
            else
            {
                logger.LogError("allowed parameters: delete <replayHash>");
                return;
            }
        }
        else
        {
            logger.LogError("allowed parameters: dsstats|sc2arcade|combo");
            return;
        }

        sw.Stop();
        logger.LogWarning("job done in {ms} ms ({min} min)", sw.ElapsedMilliseconds, Math.Round(sw.Elapsed.TotalMinutes, 2));
    }

    private static void DeleteReplay(string replayHash, ReplayContext context, ILogger<Program> logger)
    {
        try
        {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            var replay = context.Replays
                .Include(i => i.ReplayPlayers)
                    .ThenInclude(i => i.Spawns)
                        .ThenInclude(i => i.Units)
                .Include(i => i.ReplayRatingInfo)
                    .ThenInclude(i => i.RepPlayerRatings)
                .Include(i => i.ComboReplayRating)
                .Include(i => i.ReplayPlayers)
                    .ThenInclude(i => i.ComboReplayPlayerRating)
                .FirstOrDefault(f => f.ReplayHash == replayHash);
#pragma warning restore CS8602 // Dereference of a possibly null reference.

            if (replay is not null)
            {
                context.Replays.Remove(replay);
                context.SaveChanges();
                logger.LogWarning("replay {hash} removed.", replayHash);
            }
            else
            {
                logger.LogWarning("replay {hash} not found.", replayHash);
            }
        }
        catch (Exception ex)
        {
            logger.LogError("failed removing replay {hash}: {error}", replayHash, ex.Message);
        }
    }

    private static void SetLastSpawnHashes(ServiceProvider serviceProvider)
    {
        Dictionary<string, string> lastSpawnHashes = new();
        Dictionary<string, string> lsDups = new();

        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        MD5 md5Hash = MD5.Create();

        int skip = 0;
        int take = 2500;

        var replays = context.Replays
            .Include(i => i.ReplayPlayers)
                .ThenInclude(i => i.Spawns)
                    .ThenInclude(i => i.Units)
                        .ThenInclude(i => i.Unit)
            .Include(i => i.ReplayPlayers)
                .ThenInclude(i => i.Player)
            .OrderByDescending(o => o.GameTime)
                .ThenBy(o => o.ReplayId)
            .Where(x => x.GameTime > new DateTime(2021, 2, 1))
            .Skip(skip)
            .Take(take)
            .ToList();

        while (replays.Count > 0)
        {
            logger.LogInformation("step {skip}/{currentCount}", skip, lsDups.Count);

            foreach (var replay in replays)
            {
                foreach (var rp in replay.ReplayPlayers)
                {
                    var spawn = rp.Spawns.FirstOrDefault(f => f.Breakpoint == Breakpoint.All);
                    if (spawn is null)
                    {
                        continue;
                    }
                    var lastSpawnHash = spawn.GenHashV3(replay, rp, md5Hash);

                    if (lastSpawnHashes.TryAdd(lastSpawnHash, replay.ReplayHash))
                    {
                        rp.LastSpawnHash = lastSpawnHash;
                    }
                    else
                    {
                        lsDups.TryAdd(replay.ReplayHash, lastSpawnHashes[lastSpawnHash]);
                        break;
                    }
                }
            }
            context.SaveChanges();

            skip += take;
            replays = context.Replays
                .Include(i => i.ReplayPlayers)
                    .ThenInclude(i => i.Spawns)
                        .ThenInclude(i => i.Units)
                            .ThenInclude(i => i.Unit)
                .Include(i => i.ReplayPlayers)
                    .ThenInclude(i => i.Player)
                .OrderByDescending(o => o.GameTime)
                    .ThenBy(o => o.ReplayId)
                .Where(x => x.GameTime > new DateTime(2021, 2, 1))
                .Skip(skip)
                .Take(take)
                .ToList();
        }
    }

    private static void CheckLastSpawnHashDuplicates(ServiceProvider serviceProvider)
    {
        using var mscope = serviceProvider.CreateScope();
        var logger = mscope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        MD5 md5Hash = MD5.Create();

        var lsDups = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText("/data/ds/lsdups.json")) ?? new();

        foreach (var ent in lsDups)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

            var replay1 = context.Replays
                .Include(i => i.ReplayPlayers)
                    .ThenInclude(i => i.Spawns)
                        .ThenInclude(i => i.Units)
                            .ThenInclude(i => i.Unit)
                .Include(i => i.ReplayPlayers)
                    .ThenInclude(i => i.Player)
                .Where(x => x.ReplayHash == ent.Key)
                .First();

            var replay2 = context.Replays
                .Include(i => i.ReplayPlayers)
                    .ThenInclude(i => i.Spawns)
                        .ThenInclude(i => i.Units)
                            .ThenInclude(i => i.Unit)
                .Include(i => i.ReplayPlayers)
                    .ThenInclude(i => i.Player)

                .Where(x => x.ReplayHash == ent.Value)
                .First();

            if (Math.Abs((replay1.GameTime - replay2.GameTime).TotalDays) > 1)
            {
                logger.LogWarning("dup not plausible: {hash1}|{hash2}", replay1.ReplayHash, replay2.ReplayHash);
                continue;
            }

            var keepReplay = replay1;
            var throwReplay = replay2;

            if (replay2.Duration > replay1.Duration)
            {
                keepReplay = replay2;
                throwReplay = replay1;
            }

            logger.LogInformation("keepReplay: {hash1}, throwReplay: {hash2}", keepReplay.ReplayHash, throwReplay.ReplayHash);

            bool isPlausible = true;
            string uploaderPlayerString = string.Empty;
            foreach (var rp in throwReplay.ReplayPlayers)
            {
                var playerId = new PlayerId(rp.Player.ToonId, rp.Player.RealmId, rp.Player.RegionId);
                var dbRp = keepReplay.ReplayPlayers
                    .FirstOrDefault(f => new PlayerId(f.Player.ToonId, f.Player.RealmId, f.Player.RegionId) == playerId);

                if (dbRp is null)
                {
                    logger.LogWarning("db replay player not found! {hash1}{hash2} => pos: {pos}", replay1.ReplayHash, replay2.ReplayHash, rp.GamePos);
                    isPlausible = false;
                    break;
                }

                if (rp.IsUploader)
                {
                    dbRp.IsUploader = true;
                    uploaderPlayerString = Data.GetPlayerIdString(playerId) ?? "";
                }

                var spawn = rp.Spawns.FirstOrDefault(f => f.Breakpoint == Breakpoint.All);
                if (spawn is null)
                {
                    continue;
                }
                var lastSpawnHash = spawn.GenHashV3(keepReplay, dbRp, md5Hash);
                dbRp.LastSpawnHash = lastSpawnHash;
            }

            if (!isPlausible)
            {
                continue;
            }

            logger.LogWarning("deleting replay {hash} with uploader {uploader}", throwReplay.ReplayHash, uploaderPlayerString);
            DeleteReplay(throwReplay.ReplayHash, context, logger);
            context.SaveChanges();
        }
    }
}
