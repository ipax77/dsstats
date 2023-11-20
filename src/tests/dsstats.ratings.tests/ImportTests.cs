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
public class ImportTests
{
    private ServiceProvider serviceProvider;
    private readonly List<RequestNames> playerPool;
    private readonly List<UpgradeDto> upgradePool;
    private readonly List<UnitDto> unitPool;
    private readonly int poolCount = 100;

    public ImportTests()
    {
        // seed test player pool
        playerPool = new();
        upgradePool = new();
        unitPool = new();

        for (int i = 2; i < poolCount + 2; i++)
        {
            playerPool.Add(new($"Test{i}", i, 1, 1));
            upgradePool.Add(new()
            {
                Name = $"Upgrade{i}"
            });
            unitPool.Add(new()
            {
                Name = $"Unit{i}"
            });
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
    public void T01BasicImportTest()
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        var replayRepository = scope.ServiceProvider.GetRequiredService<IReplayRepository>();
        var importService = scope.ServiceProvider.GetRequiredService<ImportService>();

        context.Database.EnsureDeleted();
        context.Database.Migrate();

        using var md5 = MD5.Create();

        int replayCount = 100;
        List<ReplayDto> replayDtos = new();
        for (int i = 0; i < replayCount; i++)
        {
            replayDtos.Add(GetBasicReplayDto(md5));
        }

        var playerIds = replayDtos.SelectMany(s => s.ReplayPlayers)
            .Select(s => new PlayerId(s.Player.ToonId, s.Player.RealmId, s.Player.RegionId))
            .Distinct()
            .Count();

        importService.Import(replayDtos).Wait();

        var dbPlayers = context.Players.Count();
        Assert.AreEqual(playerIds, dbPlayers);

        var dbReplays = context.Replays.Count();
        Assert.AreEqual(replayCount, dbReplays);

        var units = context.Units.Count();
        Assert.IsTrue(units <= poolCount);

        var upgrades = context.Upgrades.Count();
        Assert.IsTrue(upgrades <= poolCount);
    }

    [TestMethod]
    public void T02AdvancedImportTest()
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        var replayRepository = scope.ServiceProvider.GetRequiredService<IReplayRepository>();
        var importService = scope.ServiceProvider.GetRequiredService<ImportService>();

        using var md5 = MD5.Create();

        var replayCountBefore = context.Replays.Count();
        int replayImportCount = 100;
        List<ReplayDto> replayDtos1 = new();
        List<ReplayDto> replayDtos2 = new();

        for (int i = 0; i < replayImportCount; i++)
        {
            replayDtos1.Add(GetBasicReplayDto(md5));
            replayDtos2.Add(GetBasicReplayDto(md5));
        }

        var task1 = importService.Import(replayDtos1);
        var task2 = importService.Import(replayDtos2);
        Task[] tasks = [task1, task2];
        Task.WaitAll(tasks);

        var dbPlayers = context.Players.Count();
        Assert.IsTrue(dbPlayers <= poolCount);

        var dbReplays = context.Replays.Count();
        Assert.AreEqual(replayCountBefore + (replayImportCount * 2), dbReplays);

        var units = context.Units.Count();
        Assert.IsTrue(units <= poolCount);

        var upgrades = context.Upgrades.Count();
        Assert.IsTrue(upgrades <= poolCount);
    }

    [TestMethod]
    public void T03ImportPreRatingTest()
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        var replayRepository = scope.ServiceProvider.GetRequiredService<IReplayRepository>();
        var importService = scope.ServiceProvider.GetRequiredService<ImportService>();

        context.Database.EnsureDeleted();
        context.Database.Migrate();

        using var md5 = MD5.Create();

        var replayDto = GetBasicReplayDto(md5);

        importService.Import([replayDto]).Wait();

        var replay = context.Replays
            .Include(i => i.ReplayRatingInfo)
            .Include(i => i.ComboReplayRating)
            .FirstOrDefault(f => f.ReplayHash == replayDto.ReplayHash);

        Assert.IsNotNull(replay);
        Assert.IsNotNull(replay.ReplayRatingInfo);
        Assert.IsNotNull(replay.ComboReplayRating);
        Assert.IsTrue(replay.ReplayRatingInfo.IsPreRating);
        Assert.IsTrue(replay.ComboReplayRating.IsPreRating);
    }

    [TestMethod]
    public void T04ImportPreRatingDoubleTest()
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        var replayRepository = scope.ServiceProvider.GetRequiredService<IReplayRepository>();
        var importService = scope.ServiceProvider.GetRequiredService<ImportService>();

        using var md5 = MD5.Create();

        var replayDto1 = GetBasicReplayDto(md5);

        importService.Import([replayDto1]).Wait();

        var replay1 = context.Replays
            .Include(i => i.ReplayRatingInfo)
            .Include(i => i.ComboReplayRating)
            .FirstOrDefault(f => f.ReplayHash == replayDto1.ReplayHash);

        Assert.IsNotNull(replay1);
        Assert.IsNotNull(replay1.ReplayRatingInfo);
        Assert.IsNotNull(replay1.ComboReplayRating);
        Assert.IsTrue(replay1.ReplayRatingInfo.IsPreRating);
        Assert.IsTrue(replay1.ComboReplayRating.IsPreRating);

        var replayDto2 = GetBasicReplayDto(md5);

        importService.Import([replayDto2]).Wait();

        var replay2 = context.Replays
            .Include(i => i.ReplayRatingInfo)
            .Include(i => i.ComboReplayRating)
            .FirstOrDefault(f => f.ReplayHash == replayDto2.ReplayHash);

        Assert.IsNotNull(replay2);
        Assert.IsNotNull(replay2.ReplayRatingInfo);
        Assert.IsNotNull(replay2.ComboReplayRating);
        Assert.IsTrue(replay2.ReplayRatingInfo.IsPreRating);
        Assert.IsTrue(replay2.ComboReplayRating.IsPreRating);

        var ratings2 = context.ComboReplayRatings
            .Where(x => x.ReplayId == replay2.ReplayId)
            .ToList();

        Assert.AreEqual(1, ratings2.Count);

        var ratings1 = context.ComboReplayRatings
            .Where(x => x.ReplayId == replay1.ReplayId)
            .ToList();

        Assert.AreEqual(1, ratings2.Count);
    }

    [TestMethod]
    public void T05ImportPreRatingStdTest()
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        var replayRepository = scope.ServiceProvider.GetRequiredService<IReplayRepository>();
        var importService = scope.ServiceProvider.GetRequiredService<ImportService>();

        using var md5 = MD5.Create();

        var replayDto = GetBasicReplayDto(md5, GameMode.Standard);

        importService.Import([replayDto]).Wait();

        var replay = context.Replays
            .Include(i => i.ReplayRatingInfo)
            .Include(i => i.ComboReplayRating)
            .FirstOrDefault(f => f.ReplayHash == replayDto.ReplayHash);

        Assert.IsNotNull(replay);
        Assert.IsNotNull(replay.ReplayRatingInfo);
        Assert.IsNotNull(replay.ComboReplayRating);
        Assert.IsTrue(replay.ReplayRatingInfo.IsPreRating);
        Assert.IsTrue(replay.ComboReplayRating.IsPreRating);
    }

    [TestMethod]
    public void T06AdvancedImportPreRatingTest()
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        var replayRepository = scope.ServiceProvider.GetRequiredService<IReplayRepository>();
        var importService = scope.ServiceProvider.GetRequiredService<ImportService>();

        using var md5 = MD5.Create();

        var replayCountBefore = context.Replays.Count();
        int replayImportCount = 4;
        List<ReplayDto> replayDtos1 = new();
        List<ReplayDto> replayDtos2 = new();

        for (int i = 0; i < replayImportCount; i++)
        {
            replayDtos1.Add(GetBasicReplayDto(md5));
            replayDtos2.Add(GetBasicReplayDto(md5));
        }

        var task1 = importService.Import(replayDtos1);
        var task2 = importService.Import(replayDtos2);
        Task[] tasks = [task1, task2];
        Task.WaitAll(tasks);

        var replayDto = replayDtos2[Random.Shared.Next(0, replayDtos2.Count)];
        var replay = context.Replays
            .FirstOrDefault(f => f.ReplayHash == replayDto.ReplayHash);

        Assert.IsNotNull(replay);

        var ratings = context.ComboReplayRatings
            .Where(x => x.ReplayId == replay.ReplayId)
            .ToList();

        Assert.AreEqual(1, ratings.Count);
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
            CommandersTeam1 = gameMode == GameMode.Standard ? "|1|1|1|" : "|10|10|10|",
            CommandersTeam2 = "|10|10|10|",
            Playercount = 6,
            Middle = "",
            ReplayPlayers = GetBasicReplayPlayerDtos(gameMode).ToList()
        };
        replay.GenHash(md5);
        return replay;
    }

    public ReplayPlayerDto[] GetBasicReplayPlayerDtos(GameMode gameMode)
    {
        var players = GetDefaultPlayers();
        return players.Select((s, i) => new ReplayPlayerDto()
        {
            Name = "Test",
            GamePos = i + 1,
            Team = i + 1 <= 3 ? 1 : 2,
            PlayerResult = i + 1 <= 3 ? PlayerResult.Win : PlayerResult.Los,
            Duration = 500,
            Race = gameMode == GameMode.Standard ? Commander.Protoss : Commander.Abathur,
            OppRace = gameMode == GameMode.Standard ? Commander.Protoss : Commander.Abathur,
            Income = Random.Shared.Next(1500, 3000),
            Army = Random.Shared.Next(1500, 3000),
            Kills = Random.Shared.Next(1500, 3000),
            TierUpgrades = "",
            Refineries = "",
            Player = s,
            Upgrades = GetDefaultUpgrades().Select(s => new PlayerUpgradeDto()
            {
                Gameloop = Random.Shared.Next(10, 11200),
                Upgrade = s
            }).ToList(),
            Spawns = new List<SpawnDto>() { GetDefaultSpawn() }
        }).ToArray();
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

    public List<UpgradeDto> GetDefaultUpgrades()
    {
        List<UpgradeDto> upgrades = new();
        for (int i = 0; i < 3; i++)
        {
            var upgrade = upgradePool[Random.Shared.Next(0, upgradePool.Count)];
            upgrades.Add(upgrade);
        }
        return upgrades;
    }

    public List<UnitDto> GetDefaultUnits()
    {
        List<UnitDto> units = new();
        for (int i = 0; i < Random.Shared.Next(3, 20); i++)
        {
            var unit = unitPool[Random.Shared.Next(0, unitPool.Count)];
            units.Add(unit);
        }
        return units;
    }

    public SpawnDto GetDefaultSpawn()
    {
        var units = GetDefaultUnits();
        return new()
        {
            Gameloop = 11200,
            Breakpoint = Breakpoint.All,
            Income = Random.Shared.Next(1000, 3000),
            GasCount = Random.Shared.Next(0, 3),
            ArmyValue = Random.Shared.Next(3000, 6000),
            KilledValue = Random.Shared.Next(3000, 6000),
            UpgradeSpent = Random.Shared.Next(500, 1500),
            Units = units.Select(s => new SpawnUnitDto()
            {
                Count = (byte)Random.Shared.Next(1, 254),
                Poss = "1,2,3,4,5,6,7,8",
                Unit = s
            }).ToList()
        };
    }
}
