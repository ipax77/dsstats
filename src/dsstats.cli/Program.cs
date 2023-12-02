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
using AutoMapper.QueryableExtensions;
using dsstats.shared.Extensions;

namespace dsstats.cli;

class Program
{
    static void Main(string[] args)
    {
        var services = new ServiceCollection();

        var serverVersion = new MySqlServerVersion(new Version(5, 7, 44));
        var jsonStrg = File.ReadAllText("/data/localserverconfig.json");
        var json = JsonSerializer.Deserialize<JsonElement>(jsonStrg);
        var config = json.GetProperty("ServerConfig");
        // var connectionString = config.GetProperty("DsstatsConnectionString").GetString();
        var connectionString = config.GetProperty("ProdConnectionString").GetString();
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

        // TestReplayHashV2(serviceProvider);
        // CheckDups("92722fb3aa4fef611ebb9896702e821a", "b28ca9eee507e3729612ba1489f1ba00", serviceProvider);
        // CheckLastSpawnHash("92722fb3aa4fef611ebb9896702e821a", "b28ca9eee507e3729612ba1489f1ba00", serviceProvider);
        TestLastSpawnHash(serviceProvider);

        sw.Stop();
        Console.WriteLine($"job done in {sw.ElapsedMilliseconds} ms.");

        Console.ReadLine();
    }

    public static void TestLastSpawnHash(ServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        Dictionary<string, KeyValuePair<string, int>> replayHashes = new();
        MD5 md5hash = MD5.Create();
        int skip = 0;
        int take = 2000;

        var replays = context.Replays
            .Include(i => i.ReplayPlayers)
                .ThenInclude(i => i.Player)
            .OrderByDescending(o => o.GameTime)
                .ThenBy(o => o.ReplayId)
            .Skip(skip)
            .Take(take)
            .ProjectTo<ReplayDto>(mapper.ConfigurationProvider)
            .AsSplitQuery()
            .ToList();

        //while (replays.Count > 0)
        //{

        foreach (var replay in replays)
        {
            foreach (var rp in replay.ReplayPlayers)
            {
                var lastSpawnHash = rp.Spawns.FirstOrDefault(f => f.Breakpoint == Breakpoint.All)?.GenHash(replay);

                if (lastSpawnHash is null)
                {
                    // logger.LogWarning("lastSpawnHash is null {hash}, {pos}", replay.ReplayHash, rp.GamePos);
                    continue;
                }

                if (!replayHashes.TryAdd(lastSpawnHash, new(replay.ReplayHash, rp.GamePos)))
                {
                    var ent = replayHashes[lastSpawnHash];
                    logger.LogError("lastSpawnHash dup: {hash1}|{hash2}, {pos}", replay.ReplayHash, ent.Key, ent.Value);
                }
            }
        }

        //skip += take;
        //replays = context.Replays
        //.Include(i => i.ReplayPlayers)
        //    .ThenInclude(i => i.Player)
        //.AsNoTracking()
        //.OrderByDescending(o => o.GameTime)
        //    .ThenBy(o => o.ReplayId)
        //.Skip(skip)
        //.Take(take)
        //.ToList();
        //}
    }

    public static void TestReplayHashV2(ServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        Dictionary<string, KeyValuePair<string, DateTime>> replayHashes = new();
        MD5 md5hash = MD5.Create();
        int skip = 0;
        int take = 5000;

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
                var oldReplayHash = replay.ReplayHash;
                replay.GenHashV2(md5hash);
                if (!replayHashes.TryAdd(replay.ReplayHash, new(oldReplayHash, replay.GameTime)))
                {
                    // logger.LogWarning("failed adding replay hash {replayHash}, {gameTime} <=> {hashGameTime}, {oldHash1}|{oldHash2}",
                    //     replay.ReplayHash, replay.GameTime, replayHashes[replay.ReplayHash].Value, oldReplayHash, replayHashes[replay.ReplayHash].Key);
                    CheckDups(oldReplayHash, replayHashes[replay.ReplayHash].Key, context, mapper, md5hash, logger);
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

    public static void CheckDups(string hash1, string hash2, ReplayContext context, IMapper mapper, MD5 md5Hash, ILogger<Program> logger)
    {
        var replay1 = context.Replays
            .Where(x => x.ReplayHash == hash1)
            .ProjectTo<ReplayDto>(mapper.ConfigurationProvider)
            .First();

        var replay2 = context.Replays
            .Where(x => x.ReplayHash == hash2)
            .ProjectTo<ReplayDto>(mapper.ConfigurationProvider)
            .First();

        // string deleteReplayHash;

        if (DuplicateIsPlausible(replay1, replay2))
        {
            logger.LogWarning("duplicate plausible: {hash1}|{hash2}", hash1, hash2);
        }
        else
        {
            logger.LogError("duplicate not plausible: {hash1}|{hash2}", hash1, hash2);
        }
    }

    public static void CheckLastSpawnHash(string hash1, string hash2, ServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        var replay1 = context.Replays
            .Where(x => x.ReplayHash == hash1)
            .ProjectTo<ReplayDto>(mapper.ConfigurationProvider)
            .First();

        var replay2 = context.Replays
            .Where(x => x.ReplayHash == hash2)
            .ProjectTo<ReplayDto>(mapper.ConfigurationProvider)
            .First();

        logger.LogInformation("rep1:");
        foreach (var rp in replay1.ReplayPlayers.OrderBy(o => o.GamePos))
        {
            var lastSpawnHash = rp.Spawns.First(f => f.Breakpoint == Breakpoint.All)?.GenHash(replay1);
            logger.LogInformation("{pos} => {hash}", rp.GamePos, lastSpawnHash);
        }

        logger.LogInformation("rep2:");
        foreach (var rp in replay2.ReplayPlayers.OrderBy(o => o.GamePos))
        {
            var lastSpawnHash = rp.Spawns.First(f => f.Breakpoint == Breakpoint.All)?.GenHash(replay2);
            logger.LogInformation("{pos} => {hash}", rp.GamePos, lastSpawnHash);
        }
    }

    private static bool DuplicateIsPlausible(ReplayDto replay1, ReplayDto replay2)
    {
        if (replay1.Duration < 60 && replay2.Duration < 60)
        {
            return true; // we don't care
        }

        if (replay1.Playercount == replay2.Playercount && replay1.Playercount == 1)
        {
            return true; // we don't care
        }

        if (Math.Abs(replay1.Duration - replay2.Duration) > 90)
        {
            return false;
        }

        if (Math.Abs((replay1.GameTime - replay2.GameTime).TotalHours) > 8)
        {
            return false;
        }
        return true;
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
