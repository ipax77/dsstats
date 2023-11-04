using dsstats.db8;
using dsstats.db8.AutoMapper;
using dsstats.db8services;
using dsstats.ratings.lib;
using dsstats.shared;
using dsstats.shared.Calc;
using dsstats.shared.Extensions;
using dsstats.shared.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Cryptography;

namespace dsstats.ratings.tests;

[TestClass]
public class RatingsSqliteTests
{
    ServiceProvider serviceProvider;

    public RatingsSqliteTests()
    {
        var services = new ServiceCollection();

        var connectionString = "DataSource=/data/dsreplaystest.db";

        services.AddOptions<DbImportOptions>()
            .Configure(x =>
            {
                x.ImportConnectionString = connectionString;
                x.IsSqlite = true;
            });

        services.AddDbContext<ReplayContext>(options =>
        {
            options.UseSqlite(connectionString, p =>
            {
                p.CommandTimeout(300);
                p.MigrationsAssembly("SqliteMigrations");
                p.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
            });
        });

        services.AddLogging();
        services.AddMemoryCache();
        services.AddAutoMapper(typeof(AutoMapperProfile));

        services.AddSingleton<CalcService>();

        services.AddScoped<ICalcRepository, CalcRepository>();
        services.AddScoped<IReplayRepository, ReplayRepository>();

        serviceProvider = services.BuildServiceProvider();

    }

    [TestMethod]
    public void T01BasicRatingTest()
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        var replayRepository = scope.ServiceProvider.GetRequiredService<IReplayRepository>();
        var calcService = scope.ServiceProvider.GetRequiredService<CalcService>();

        context.Database.EnsureDeleted();
        context.Database.Migrate();

        using var md5 = MD5.Create();
        var replay = GetBasicReplayDto(md5);
        replayRepository.SaveReplay(replay, new(), new()).Wait();

        var playerCount = context.Players.Count();
        Assert.AreEqual(6, playerCount);

        calcService.GenerateRatings(RatingCalcType.Dsstats, true).Wait();

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
        var calcService = scope.ServiceProvider.GetRequiredService<CalcService>();


        using var md5 = MD5.Create();
        for (int i = 0; i < 102; i++)
        {
            var replay = GetBasicReplayDto(md5);
            replayRepository.SaveReplay(replay, new(), new()).Wait();
        }
        var result = calcService.GenerateRatings(RatingCalcType.Dsstats, true).GetAwaiter().GetResult();

        var replayRatings = context.ReplayRatings.Count();

        Assert.IsTrue(replayRatings >= 100);
    }

    [TestMethod]
    public void T03ContinueRatingTest()
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        var replayRepository = scope.ServiceProvider.GetRequiredService<IReplayRepository>();
        var calcService = scope.ServiceProvider.GetRequiredService<CalcService>();

        var replayRatingsBefore = context.ReplayRatings
            .Count();

        using var md5 = MD5.Create();
        for (int i = 0; i < 10; i++)
        {
            var replay = GetBasicReplayDto(md5);
            replayRepository.SaveReplay(replay, new(), new()).Wait();
        }
        var result = calcService.GenerateRatings(RatingCalcType.Dsstats).GetAwaiter().GetResult();

        var replayRatingsAfter = context.ReplayRatings
            .Count();

        Assert.AreEqual(replayRatingsAfter, replayRatingsBefore + 10);
    }

    [TestMethod]
    public void T04ContinueRecalcRatingTest()
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        var replayRepository = scope.ServiceProvider.GetRequiredService<IReplayRepository>();
        var calcService = scope.ServiceProvider.GetRequiredService<CalcService>();

        DateTime startTime = DateTime.UtcNow;

        var player = GetBasicPlayerDto();

        using var md5 = MD5.Create();
        for (int i = 0; i < 10; i++)
        {
            var replay = GetBasicReplayDto(md5);
            var pl1 = replay.ReplayPlayers.Where(x => x.GamePos == 1).First();
            var testpl = pl1 with { Player = player };
            replay.ReplayPlayers.Remove(pl1);
            replay.ReplayPlayers.Add(testpl);

            replayRepository.SaveReplay(replay, new(), new()).Wait();
        }
        var continueResult = calcService.GenerateRatings(RatingCalcType.Dsstats).GetAwaiter().GetResult();

        var testPlContinueRating = context.PlayerRatings
            .Where(x => x.Player.ToonId == player.ToonId
                && x.Player.RealmId == player.RealmId
                && x.Player.RegionId == player.RegionId)
            .Select(s => s.Rating)
            .FirstOrDefault();

        Assert.IsTrue(testPlContinueRating > 0);

        var recalcResult = calcService.GenerateRatings(RatingCalcType.Dsstats, recalc: true).GetAwaiter().GetResult();

        var testPlRecalcRating = context.PlayerRatings
            .Where(x => x.Player.ToonId == player.ToonId
                && x.Player.RealmId == player.RealmId
                && x.Player.RegionId == player.RegionId)
            .Select(s => s.Rating)
            .FirstOrDefault();

        Assert.AreEqual(testPlRecalcRating, testPlContinueRating);
    }


    public static ReplayDto GetBasicReplayDto(MD5 md5, GameMode gameMode = GameMode.Commanders)
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
            ReplayPlayers = new List<ReplayPlayerDto>()
            {
                GetBasicReplayPlayerDto(1),
                GetBasicReplayPlayerDto(2),
                GetBasicReplayPlayerDto(3),
                GetBasicReplayPlayerDto(4),
                GetBasicReplayPlayerDto(5),
                GetBasicReplayPlayerDto(6),
            }
        };
        replay.GenHash(md5);
        return replay;
    }

    public static ReplayPlayerDto GetBasicReplayPlayerDto(int gamePos)
    {
        return new ReplayPlayerDto()
        {
            Name = "Test",
            GamePos = gamePos,
            Team = gamePos <= 3 ? 1 : 2,
            PlayerResult = gamePos <= 3 ? PlayerResult.Win : PlayerResult.Los,
            Duration = 500,
            Race = Commander.Abathur,
            OppRace = Commander.Abathur,
            Income = Random.Shared.Next(1500, 3000),
            Army = Random.Shared.Next(1500, 3000),
            Kills = Random.Shared.Next(1500, 3000),
            TierUpgrades = "",
            Refineries = "",
            Player = GetBasicPlayerDto()
        };
    }

    public static PlayerDto GetBasicPlayerDto()
    {
        return new()
        {
            Name = "Test",
            ToonId = Random.Shared.Next(1, 1000000),
            RegionId = 1,
            RealmId = 1,
        };
    }
}