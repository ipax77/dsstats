using dsstats.db8;
using dsstats.db8.AutoMapper;
using dsstats.db8services;
using dsstats.db8services.Import;
using dsstats.shared;
using dsstats.shared.Calc;
using dsstats.shared.Extensions;
using dsstats.shared.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Cryptography;
using System.Text.Json;

namespace dsstats.ratings.tests;

[TestClass]
public class RatingsTests
{
    ServiceProvider serviceProvider;
    private readonly List<RequestNames> playerPool;
    private readonly int poolCount = 100;

    public RatingsTests()
    {
        playerPool = new();

        for (int i = 2; i < poolCount + 2; i++)
        {
            playerPool.Add(new($"Test{i}", i, 1, 1));
        }

        var services = new ServiceCollection();
        var serverVersion = new MySqlServerVersion(new Version(5, 7, 43));
        var jsonStrg = File.ReadAllText("/data/localserverconfig.json");
        var json = JsonSerializer.Deserialize<JsonElement>(jsonStrg);
        var config = json.GetProperty("ServerConfig");
        var connectionString = config.GetProperty("TestConnectionString").GetString();
        var importConnectionString = config.GetProperty("ImportTestConnectionString").GetString() ?? "";

        services.AddOptions<DbImportOptions>()
            .Configure(x =>
            {
                x.ImportConnectionString = importConnectionString;
                x.IsSqlite = false;
            });

        services.AddDbContext<ReplayContext>(options =>
        {
            options.UseMySql(connectionString, serverVersion, p =>
            {
                p.CommandTimeout(300);
                p.MigrationsAssembly("MysqlMigrations");
                p.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
            });
        });

        services.AddLogging();
        services.AddMemoryCache();
        services.AddAutoMapper(typeof(AutoMapperProfile));

        services.AddSingleton<IRatingService, RatingService>();
        services.AddSingleton<IRatingsSaveService, RatingsSaveService>();
        services.AddSingleton<ImportService>();
        services.AddScoped<IReplayRepository, ReplayRepository>();


        serviceProvider = services.BuildServiceProvider();

    }

    [TestMethod]
    public void T01BasicRatingTest()
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        var replayRepository = scope.ServiceProvider.GetRequiredService<IReplayRepository>();
        var ratingService = scope.ServiceProvider.GetRequiredService<IRatingService>();

        context.Database.EnsureDeleted();
        context.Database.Migrate();

        using var md5 = MD5.Create();
        var replay = GetBasicReplayDto(md5);
        replayRepository.SaveReplay(replay, new(), new()).Wait();

        var playerCount = context.Players.Count();
        Assert.AreEqual(6, playerCount);

        ratingService.ProduceRatings(RatingCalcType.Dsstats, true).Wait();

        var ratings = context.PlayerRatings
            .Where(x => x.PlayerId > 0)
            .ToList();

        Assert.AreEqual(6, ratings.Count);

        MmrOptions options = new MmrOptions();

        var ratingsAboveDefault = ratings
            .Where(x => x.Rating > options.StartMmr)
            .Count();

        var ratingsBelowDefault = ratings
            .Where(x => x.Rating < options.StartMmr)
            .Count();

        Assert.AreEqual(3, ratingsAboveDefault);
        Assert.AreEqual(3, ratingsBelowDefault);
    }

    [TestMethod]
    public void T02AdvancedRatingTest()
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        var replayRepository = scope.ServiceProvider.GetRequiredService<IReplayRepository>();
        var ratingService = scope.ServiceProvider.GetRequiredService<IRatingService>();


        using var md5 = MD5.Create();
        for (int i = 0; i < 102; i++)
        {
            var replay = GetBasicReplayDto(md5);
            replayRepository.SaveReplay(replay, new(), new()).Wait();
        }
        ratingService.ProduceRatings(RatingCalcType.Dsstats, true).GetAwaiter().GetResult();

        var replayRatings = context.ReplayRatings.Count();

        Assert.IsTrue(replayRatings >= 100);
    }

    [TestMethod]
    public void T03ContinueRatingTest()
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        var replayRepository = scope.ServiceProvider.GetRequiredService<IReplayRepository>();
        var ratingService = scope.ServiceProvider.GetRequiredService<IRatingService>();

        var replayRatingsBefore = context.ReplayRatings
            .Count();

        using var md5 = MD5.Create();
        for (int i = 0; i < 10; i++)
        {
            var replay = GetBasicReplayDto(md5);
            replayRepository.SaveReplay(replay, new(), new()).Wait();
        }
        ratingService.ProduceRatings(RatingCalcType.Dsstats).GetAwaiter().GetResult();

        var replayRatingsAfter = context.ReplayRatings
            .Count();

        Assert.AreEqual(replayRatingsBefore + 10, replayRatingsAfter);
    }

    [TestMethod]
    public void T04ContinueRecalcRatingTest()
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        var replayRepository = scope.ServiceProvider.GetRequiredService<IReplayRepository>();
        var ratingService = scope.ServiceProvider.GetRequiredService<IRatingService>();

        DateTime startTime = DateTime.UtcNow;

        var player = GetBasicPlayerDto();

        using var md5 = MD5.Create();
        for (int i = 0; i < 10; i++)
        {
            var replay = GetBasicReplayDto(md5);
            var testPl = replay.ReplayPlayers.Where(x => x.Player == player).FirstOrDefault();
            if (testPl is null)
            {
                var pl1 = replay.ReplayPlayers.Where(x => x.GamePos == 1).First();
                var testpl = pl1 with { Player = player };
                replay.ReplayPlayers.Remove(pl1);
                replay.ReplayPlayers.Add(testpl);
            }
            replayRepository.SaveReplay(replay, new(), new()).Wait();
        }
        ratingService.ProduceRatings(RatingCalcType.Dsstats).GetAwaiter().GetResult();

        var testPlContinueRating = context.PlayerRatings
            .Where(x => x.Player.ToonId == player.ToonId
                && x.Player.RealmId == player.RealmId
                && x.Player.RegionId == player.RegionId)
            .Select(s => s.Rating)
            .FirstOrDefault();

        Assert.IsTrue(testPlContinueRating > 0);

        ratingService.ProduceRatings(RatingCalcType.Dsstats, recalc: true).GetAwaiter().GetResult();

        var testPlRecalcRating = context.PlayerRatings
            .Where(x => x.Player.ToonId == player.ToonId
                && x.Player.RealmId == player.RealmId
                && x.Player.RegionId == player.RegionId)
            .Select(s => s.Rating)
            .FirstOrDefault();

        Assert.AreEqual(testPlRecalcRating, testPlContinueRating);
    }



    //[TestMethod]
    //public void T05NothingToDoTest()
    //{
    //    using var scope = serviceProvider.CreateScope();
    //    var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
    //    var ratingService = scope.ServiceProvider.GetRequiredService<IRatingService>();

    //    ratingService.ProduceRatings(RatingCalcType.Dsstats).GetAwaiter().GetResult();
    //    Assert.IsTrue(result.NothingToDo);
    //}

    [TestMethod]
    public void T06PlayerMultiRatingTest()
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        var replayRepository = scope.ServiceProvider.GetRequiredService<IReplayRepository>();
        var ratingService = scope.ServiceProvider.GetRequiredService<IRatingService>();

        using var md5 = MD5.Create();

        var player = GetBasicPlayerDto();

        var cmdrReplay = GetBasicReplayDto(md5);
        var firstPlayer = cmdrReplay.ReplayPlayers.First();
        cmdrReplay.ReplayPlayers.Remove(firstPlayer);
        cmdrReplay.ReplayPlayers.Add(firstPlayer with { Player = player });
        replayRepository.SaveReplay(cmdrReplay, new(), new()).Wait();

        var stdReplay = GetBasicReplayDto(md5, GameMode.Standard);
        var firstStdPlayer = stdReplay.ReplayPlayers.First();
        stdReplay.ReplayPlayers.Remove(firstStdPlayer);
        stdReplay.ReplayPlayers.Add(firstStdPlayer with { Player = player });
        replayRepository.SaveReplay(stdReplay, new(), new()).Wait();

        ratingService.ProduceRatings(RatingCalcType.Dsstats, true).Wait();

        var ratings = context.PlayerRatings
            .Where(x => x.Player.ToonId == player.ToonId
                && x.Player.RealmId == player.RealmId
                && x.Player.RegionId == player.RegionId)
            .ToList();

        Assert.AreEqual(2, ratings.Count);

        var cmdrReplay2 = GetBasicReplayDto(md5);
        var firstPlayer2 = cmdrReplay2.ReplayPlayers.First();
        cmdrReplay2.ReplayPlayers.Remove(firstPlayer2);
        cmdrReplay2.ReplayPlayers.Add(firstPlayer2 with { Player = player });
        replayRepository.SaveReplay(cmdrReplay2, new(), new()).Wait();

        var stdReplay2 = GetBasicReplayDto(md5, GameMode.Standard);
        var firstStdPlayer2 = stdReplay2.ReplayPlayers.First();
        stdReplay2.ReplayPlayers.Remove(firstStdPlayer2);
        stdReplay2.ReplayPlayers.Add(firstStdPlayer2 with { Player = player });
        replayRepository.SaveReplay(stdReplay2, new(), new()).Wait();

        ratingService.ProduceRatings(RatingCalcType.Dsstats).GetAwaiter().GetResult();

        var ratings2 = context.PlayerRatings
            .Where(x => x.Player.ToonId == player.ToonId
                && x.Player.RealmId == player.RealmId
                && x.Player.RegionId == player.RegionId)
            .ToList();

        Assert.AreEqual(2, ratings2.Count);
    }

    [TestMethod]
    public void T07PlayerMultiComboRatingTest()
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        var replayRepository = scope.ServiceProvider.GetRequiredService<IReplayRepository>();
        var ratingService = scope.ServiceProvider.GetRequiredService<IRatingService>();

        using var md5 = MD5.Create();

        Uploader uploader = new()
        {
            AppGuid = Guid.NewGuid(),
        };

        context.Uploaders.Add(uploader);
        context.SaveChanges();

        var player = GetBasicPlayerDto();
        var dbPlayer = context.Players
            .FirstOrDefault(f => f.ToonId == player.ToonId
                && f.RealmId == player.RealmId
                && f.RegionId == player.RegionId);
        if (dbPlayer is null)
        {
            dbPlayer = new()
            {
                Name = player.Name,
                ToonId = player.ToonId,
                RealmId = player.RealmId,
                RegionId = player.RegionId,
            };
            context.Add(dbPlayer);
        }
        dbPlayer.UploaderId = uploader.UploaderId;
        context.SaveChanges();

        var cmdrReplay = GetBasicReplayDto(md5);
        var firstPlayer = cmdrReplay.ReplayPlayers.First();
        cmdrReplay.ReplayPlayers.Remove(firstPlayer);
        cmdrReplay.ReplayPlayers.Add(firstPlayer with { Player = player });
        replayRepository.SaveReplay(cmdrReplay, new(), new()).Wait();

        var stdReplay = GetBasicReplayDto(md5, GameMode.Standard);
        var firstStdPlayer = stdReplay.ReplayPlayers.First();
        stdReplay.ReplayPlayers.Remove(firstStdPlayer);
        stdReplay.ReplayPlayers.Add(firstStdPlayer with { Player = player });
        replayRepository.SaveReplay(stdReplay, new(), new()).Wait();

        ratingService.ProduceRatings(RatingCalcType.Dsstats, true).Wait();
        ratingService.ProduceRatings(RatingCalcType.Arcade, true).Wait();
        ratingService.ProduceRatings(RatingCalcType.Combo, true).Wait();

        var ratings = context.ComboPlayerRatings
            .Where(x => x.Player.ToonId == player.ToonId
                && x.Player.RealmId == player.RealmId
                && x.Player.RegionId == player.RegionId)
            .ToList();

        Assert.AreEqual(2, ratings.Count);

        var cmdrReplay2 = GetBasicReplayDto(md5);
        var firstPlayer2 = cmdrReplay2.ReplayPlayers.First();
        cmdrReplay2.ReplayPlayers.Remove(firstPlayer2);
        cmdrReplay2.ReplayPlayers.Add(firstPlayer2 with { Player = player });
        replayRepository.SaveReplay(cmdrReplay2, new(), new()).Wait();

        var stdReplay2 = GetBasicReplayDto(md5, GameMode.Standard);
        var firstStdPlayer2 = stdReplay2.ReplayPlayers.First();
        stdReplay2.ReplayPlayers.Remove(firstStdPlayer2);
        stdReplay2.ReplayPlayers.Add(firstStdPlayer2 with { Player = player });
        replayRepository.SaveReplay(stdReplay2, new(), new()).Wait();

        ratingService.ProduceRatings(RatingCalcType.Dsstats).Wait();
        ratingService.ProduceRatings(RatingCalcType.Arcade).Wait();
        ratingService.ProduceRatings(RatingCalcType.Combo).GetAwaiter().GetResult();

        var ratings2 = context.ComboPlayerRatings
            .Where(x => x.Player.ToonId == player.ToonId
                && x.Player.RealmId == player.RealmId
                && x.Player.RegionId == player.RegionId)
            .ToList();

        Assert.AreEqual(2, ratings2.Count);
    }

    [TestMethod]
    public void T08BasicRecalcRatingTest()
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        var ratingService = scope.ServiceProvider.GetRequiredService<IRatingService>();
        var importService = scope.ServiceProvider.GetRequiredService<ImportService>();

        using var md5 = MD5.Create();
        var replayDto = GetBasicReplayDto(md5);
        importService.Import([replayDto]).Wait();

        ratingService.ProduceRatings(RatingCalcType.Dsstats, true).Wait();

        var replay = context.Replays
            .Include(i => i.ReplayRatingInfo)
            .Include(i => i.ComboReplayRating)
            .FirstOrDefault(f => f.ReplayHash == replayDto.ReplayHash);

        Assert.IsNotNull(replay);
        Assert.IsNotNull(replay.ReplayRatingInfo);
        Assert.IsNotNull(replay.ComboReplayRating);

        var ratings = context.ReplayRatings
            .Where(x => x.ReplayId == replay.ReplayId)
            .Count();

        Assert.AreEqual(1, ratings);

        var replayDtoNext = GetBasicReplayDto(md5);
        replayDtoNext = replayDtoNext with { GameTime = DateTime.UtcNow.AddDays(-1) };
        importService.Import([replayDtoNext]).Wait();

        ratingService.ProduceRatings(RatingCalcType.Dsstats).Wait();

        var replayNext = context.Replays
            .Include(i => i.ReplayRatingInfo)
            .Include(i => i.ComboReplayRating)
            .FirstOrDefault(f => f.ReplayHash == replayDtoNext.ReplayHash);

        Assert.IsNotNull(replayNext);
        Assert.IsNotNull(replayNext.ReplayRatingInfo);

        var ratingsNext = context.ReplayRatings
            .Where(x => x.ReplayId == replayNext.ReplayId)
            .ToList();

        Assert.AreEqual(1, ratingsNext.Count);
        Assert.IsFalse(ratingsNext[0].IsPreRating);
    }

    public ReplayDto GetBasicReplayDto(MD5 md5, GameMode gameMode = GameMode.Commanders)
    {
        var replay = new ReplayDto()
        {
            FileName = "",
            GameMode = gameMode,
            GameTime = DateTime.UtcNow,
            Duration = 500,
            WinnerTeam = 1,
            Minkillsum = Random.Shared.Next(100, 1000),
            Maxkillsum = Random.Shared.Next(10000, 20000),
            Minincome = Random.Shared.Next(1000, 2000),
            Minarmy = Random.Shared.Next(1000, 2000),
            CommandersTeam1 = "|10|10|10|",
            CommandersTeam2 = "|10|10|10|",
            Playercount = 6,
            Middle = "",
            ReplayPlayers = GetBasicReplayPlayerDtos()
        };
        replay.GenHash(md5);
        return replay;
    }

    public List<ReplayPlayerDto> GetBasicReplayPlayerDtos()
    {
        var players = GetDefaultPlayers();
        return players.Select((s, i) => new ReplayPlayerDto()
        {
            Name = "Test",
            GamePos = i + 1,
            Team = i + 1 <= 3 ? 1 : 2,
            PlayerResult = i + 1 <= 3 ? PlayerResult.Win : PlayerResult.Los,
            Duration = 500,
            Race = Commander.Abathur,
            OppRace = Commander.Abathur,
            Income = Random.Shared.Next(1500, 3000),
            Army = Random.Shared.Next(1500, 3000),
            Kills = Random.Shared.Next(1500, 3000),
            TierUpgrades = "",
            Refineries = "",
            Player = s,
        }).ToList();
    }

    public PlayerDto[] GetDefaultPlayers()
    {
        var playerPool = this.playerPool.ToArray();
        Random.Shared.Shuffle(playerPool);

        return playerPool.Take(6)
            .Select(s => new PlayerDto()
            {
                Name = s.Name,
                ToonId = s.ToonId,
                RealmId = s.RealmId,
                RegionId = s.RegionId,
            })
            .ToArray();
    }

    private PlayerDto GetBasicPlayerDto()
    {
        var rPlayer = playerPool[Random.Shared.Next(0, playerPool.Count)];
        return new()
        {
            Name = rPlayer.Name,
            ToonId = rPlayer.ToonId,
            RealmId = rPlayer.RealmId,
            RegionId = rPlayer.RegionId,
        };
    }
}