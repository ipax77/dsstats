using dsstats.api.Services;
using dsstats.db8;
using dsstats.db8.AutoMapper;
using dsstats.db8services;
using dsstats.db8services.Import;
using dsstats.ratings.lib;
using dsstats.shared;
using dsstats.shared.Extensions;
using dsstats.shared.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Cryptography;
using System.Text.Json;

namespace dsstats.ratings.tests;

[TestClass]
public class PreRatingTests
{
    private ServiceProvider serviceProvider;

    public PreRatingTests()
    {
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
        services.AddSingleton<IRemoteToggleService, RemoteToggleService>();

        services.AddScoped<IReplayRepository, ReplayRepository>();

        serviceProvider = services.BuildServiceProvider();
    }

    [TestMethod]
    public void T01BasicPreRatingTest()
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        var replayRepository = scope.ServiceProvider.GetRequiredService<IReplayRepository>();
        var importService = scope.ServiceProvider.GetRequiredService<ImportService>();
        var ratingService = scope.ServiceProvider.GetRequiredService<IRatingService>();

        context.Database.EnsureDeleted();
        context.Database.Migrate();

        using var md5 = MD5.Create();

        for (int i = 0; i < 10; i++)
        {
            var replay = GetBasicReplayDto(md5);
            replayRepository.SaveReplay(replay, new(), new()).Wait();
        }

        ratingService.ProduceRatings(RatingCalcType.Dsstats, recalc: true).Wait();
        ratingService.ProduceRatings(RatingCalcType.Combo).Wait();

        for (int i = 0; i < 10; i++)
        {
            var replay = GetBasicReplayDto(md5);
            replayRepository.SaveReplay(replay, new(), new()).Wait();
        }

        importService.SetPreRatings().Wait();

        var dsstatsPreRatings = context.ReplayRatings
            .Where(x => x.IsPreRating)
            .Count();

        var comboPreRatings = context.ComboReplayRatings
            .Where(x => x.IsPreRating)
            .Count();

        Assert.AreEqual(10, dsstatsPreRatings);
        Assert.AreEqual(10, comboPreRatings);
    }

    [TestMethod]
    public void T02AddPreRatingTest()
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        var replayRepository = scope.ServiceProvider.GetRequiredService<IReplayRepository>();
        var importService = scope.ServiceProvider.GetRequiredService<ImportService>();

        var comboPreRatingsBefore = context.ComboReplayRatings
            .Where(x => x.IsPreRating)
            .Count();
        using var md5 = MD5.Create();

        for (int i = 0; i < 10; i++)
        {
            var replay = GetBasicReplayDto(md5);
            replayRepository.SaveReplay(replay, new(), new()).Wait();
        }

        importService.SetPreRatings().Wait();

        var dsstatsPreRatings = context.ReplayRatings
            .Where(x => x.IsPreRating)
            .Count();

        var comboPreRatings = context.ComboReplayRatings
            .Where(x => x.IsPreRating)
            .Count();

        Assert.AreEqual(comboPreRatingsBefore + 10, dsstatsPreRatings);
        Assert.AreEqual(comboPreRatingsBefore + 10, comboPreRatings);
    }

    [TestMethod]
    public void T03SinglePreRatingTest()
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        var replayRepository = scope.ServiceProvider.GetRequiredService<IReplayRepository>();
        var importService = scope.ServiceProvider.GetRequiredService<ImportService>();

        var comboPreRatingsBefore = context.ComboReplayRatings
            .Where(x => x.IsPreRating)
            .Count();
        using var md5 = MD5.Create();

        var replay = GetBasicReplayDto(md5);
        replayRepository.SaveReplay(replay, new(), new()).Wait();

        importService.SetPreRatings().Wait();

        var dsstatsPreRatings = context.ReplayRatings
            .Where(x => x.IsPreRating)
            .Count();

        var comboPreRatings = context.ComboReplayRatings
            .Where(x => x.IsPreRating)
            .Count();

        Assert.AreEqual(comboPreRatingsBefore + 1, dsstatsPreRatings);
        Assert.AreEqual(comboPreRatingsBefore + 1, comboPreRatings);
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
            Player = gamePos == 1 ? GetDefaultPlayer() : GetBasicPlayerDto()
        };
    }

    public static PlayerDto GetBasicPlayerDto()
    {
        return new()
        {
            Name = "Test",
            ToonId = Random.Shared.Next(2, 1000000),
            RegionId = 1,
            RealmId = 1,
        };
    }

    public static PlayerDto GetDefaultPlayer()
    {
        return new()
        {
            Name = "Test",
            ToonId = 1,
            RegionId = 1,
            RealmId = 1,
        };
    }
}
