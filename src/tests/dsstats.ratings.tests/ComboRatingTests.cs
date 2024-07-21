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
public class ComboRatingsTests
{
    ServiceProvider serviceProvider;
    private readonly List<RequestNames> playerPool;
    private readonly int poolCount = 100;

    public ComboRatingsTests()
    {
        playerPool = new();

        for (int i = 2; i < poolCount + 2; i++)
        {
            playerPool.Add(new($"Test{i}", i, 1, 1));
        }

        var services = new ServiceCollection();
        var serverVersion = new MySqlServerVersion(new Version(5, 7, 44));
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
        services.AddScoped<ComboRatings>();
        services.AddSingleton<IRatingService, RatingService>();
        services.AddSingleton<IRatingsSaveService, RatingsSaveService>();
        services.AddSingleton<IImportService, ImportService>();
        services.AddSingleton<IRemoteToggleService, RemoteToggleService>();
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

        ratingService.ProduceRatings(RatingCalcType.Combo, true).Wait();

        var ratings = context.ComboPlayerRatings
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
        ratingService.ProduceRatings(RatingCalcType.Combo, true).GetAwaiter().GetResult();

        var replayRatings = context.ComboReplayRatings.Count();

        Assert.IsTrue(replayRatings >= 100);
    }

    [TestMethod]
    public void T03ContinueRatingTest()
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        var replayRepository = scope.ServiceProvider.GetRequiredService<IReplayRepository>();
        var ratingService = scope.ServiceProvider.GetRequiredService<IRatingService>();

        var replayRatingsBefore = context.ComboReplayRatings
            .Count();

        using var md5 = MD5.Create();
        for (int i = 0; i < 10; i++)
        {
            var replay = GetBasicReplayDto(md5);
            replayRepository.SaveReplay(replay, new(), new()).Wait();
        }
        ratingService.ProduceRatings(RatingCalcType.Combo, false).GetAwaiter().GetResult();

        var replayRatingsAfter = context.ComboReplayRatings
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
        ratingService.ProduceRatings(RatingCalcType.Combo, false).GetAwaiter().GetResult();

        var testPlContinueRating = context.ComboPlayerRatings
            .Where(x => x.Player.ToonId == player.ToonId
                && x.Player.RealmId == player.RealmId
                && x.Player.RegionId == player.RegionId)
            .Select(s => s.Rating)
            .FirstOrDefault();

        Assert.IsTrue(testPlContinueRating > 0);

        ratingService.ProduceRatings(RatingCalcType.Dsstats, recalc: true).GetAwaiter().GetResult();

        var testPlRecalcRating = context.ComboPlayerRatings
            .Where(x => x.Player.ToonId == player.ToonId
                && x.Player.RealmId == player.RealmId
                && x.Player.RegionId == player.RegionId)
            .Select(s => s.Rating)
            .FirstOrDefault();

        Assert.AreEqual(testPlRecalcRating, testPlContinueRating);
    }

    [TestMethod]
    public void T05AdvancedContinueRecalcRatingTest()
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        var importService = scope.ServiceProvider.GetRequiredService<IImportService>();
        var ratingService = scope.ServiceProvider.GetRequiredService<IRatingService>();

        DateTime startTime = DateTime.UtcNow;

        var player = GetBasicPlayerDto();

        using var md5 = MD5.Create();

        List<ReplayDto> replays = new();

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
            replays.Add(replay);
        }
        importService.Import(replays).Wait();

        ratingService.ProduceRatings(RatingCalcType.Combo, true).GetAwaiter().GetResult();

        for (int i = 0; i < 5; i++)
        {
            for (int j = 0; j < Random.Shared.Next(1, 4); j++)
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
                replays.Add(replay);
                importService.Import([replay]).Wait();
            }
            ratingService.ProduceRatings(RatingCalcType.Combo, false).GetAwaiter().GetResult();
        }

        var testPlContinueRating = context.ComboPlayerRatings
            .Where(x => x.Player.ToonId == player.ToonId
                && x.Player.RealmId == player.RealmId
                && x.Player.RegionId == player.RegionId)
            .Select(s => new { s.Rating, s.Consistency, s.Confidence })
            .FirstOrDefault();

        Assert.IsTrue(testPlContinueRating?.Rating > 0);

        ratingService.ProduceRatings(RatingCalcType.Dsstats, recalc: true).GetAwaiter().GetResult();

        var testPlRecalcRating = context.ComboPlayerRatings
            .Where(x => x.Player.ToonId == player.ToonId
                && x.Player.RealmId == player.RealmId
                && x.Player.RegionId == player.RegionId)
            .Select(s => new { s.Rating, s.Consistency, s.Confidence })
            .FirstOrDefault();

        Assert.AreEqual(testPlRecalcRating?.Rating, testPlContinueRating.Rating);
        Assert.AreEqual(testPlRecalcRating?.Consistency, testPlContinueRating.Consistency);
        Assert.AreEqual(testPlRecalcRating?.Confidence, testPlContinueRating.Confidence);
    }

    public ArcadeReplayDto GetBasicArcadeReplayDto(GameMode gameMode = GameMode.Commanders)
    {
        int winnerTeam = Random.Shared.Next(1, 3);
        return new()
        {
            CreatedAt = DateTime.UtcNow,
            GameMode = gameMode,
            RegionId = 1,
            WinnerTeam = winnerTeam,
            Duration = 500,
            ArcadeReplayDsPlayers = GetBasicArcadeReplayPlayerDtos(winnerTeam)
        };
    }

    public List<ArcadeReplayDsPlayerDto> GetBasicArcadeReplayPlayerDtos(int winnerTeam)
    {
        var players = GetDefaultArcadePlayers();
        return players.Select((s, i) => new ArcadeReplayDsPlayerDto()
        {
            Name = "Test",
            SlotNumber = i + 1,
            Team = i + 1 <= 3 ? 1 : 2,
            PlayerResult = winnerTeam == 1 ? i + 1 <= 3 ? PlayerResult.Win : PlayerResult.Los : i + 1 <= 3
                ? PlayerResult.Los : PlayerResult.Win,
            Player = s,
        }).ToList();
    }

    public ReplayDto GetBasicReplayDto(MD5 md5, GameMode gameMode = GameMode.Commanders)
    {
        int winnerTeam = Random.Shared.Next(1, 3);
        var replay = new ReplayDto()
        {
            FileName = "",
            GameMode = gameMode,
            GameTime = DateTime.UtcNow,
            Duration = 500,
            WinnerTeam = winnerTeam,
            Minkillsum = Random.Shared.Next(100, 1000),
            Maxkillsum = Random.Shared.Next(10000, 20000),
            Minincome = Random.Shared.Next(1000, 2000),
            Minarmy = Random.Shared.Next(1000, 2000),
            CommandersTeam1 = "|10|10|10|",
            CommandersTeam2 = "|10|10|10|",
            Playercount = 6,
            Middle = "",
            ReplayPlayers = GetBasicReplayPlayerDtos(winnerTeam)
        };
        replay.GenHash(md5);
        return replay;
    }

    public List<ReplayPlayerDto> GetBasicReplayPlayerDtos(int winnerTeam)
    {
        var players = GetDefaultPlayers();
        return players.Select((s, i) => new ReplayPlayerDto()
        {
            Name = "Test",
            GamePos = i + 1,
            Team = i + 1 <= 3 ? 1 : 2,
            PlayerResult = winnerTeam == 1 ? i + 1 <= 3 ? PlayerResult.Win : PlayerResult.Los
                : i + 1 <= 3 ? PlayerResult.Los : PlayerResult.Win,
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

    public PlayerDto[] GetDefaultArcadePlayers()
    {
        var playerPool = this.playerPool.ToArray();
        Random.Shared.Shuffle(playerPool);

        return playerPool.Take(6)
            .Select(s => new PlayerDto()
            {
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