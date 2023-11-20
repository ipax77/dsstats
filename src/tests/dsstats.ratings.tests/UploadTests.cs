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
public class UploadTests
{
    private ServiceProvider serviceProvider;
    private readonly List<RequestNames> playerPool;
    private readonly List<UpgradeDto> upgradePool;
    private readonly List<UnitDto> unitPool;
    private readonly int poolCount = 100;

    public UploadTests()
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

        services.AddSingleton<IRatingService, RatingService>();
        services.AddSingleton<RatingsSaveService>();
        services.AddSingleton<ImportService>();
        services.AddSingleton<UploadService>();

        services.AddScoped<IReplayRepository, ReplayRepository>();

        serviceProvider = services.BuildServiceProvider();
    }

    [TestMethod]
    public void T01BasicUploadTest()
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        var uploadService = scope.ServiceProvider.GetRequiredService<UploadService>();

        context.Database.EnsureDeleted();
        context.Database.Migrate();

        using var md5 = MD5.Create();

        int replayCount = 10;
        List<ReplayDto> replayDtos = new();
        for (int i = 0; i < replayCount; i++)
        {
            replayDtos.Add(GetBasicReplayDto(md5));
        }

        var jsonString = JsonSerializer.Serialize(replayDtos);
        var base64string = UploadService.ZipAsync(jsonString).GetAwaiter().GetResult();

        var uploaderPlayer = replayDtos[0].ReplayPlayers.First().Player;

        UploadDto uploadDto = new()
        {
            AppGuid = Guid.Empty,
            Base64ReplayBlob = base64string,
            RequestNames = new List<RequestNames>()
            {
                new(uploaderPlayer.Name, uploaderPlayer.ToonId, uploaderPlayer.RegionId, uploaderPlayer.RealmId)
            }
        };

        ManualResetEvent mre = new ManualResetEvent(false);

        uploadService.BlobImported += (o, e) =>
        {
            Assert.IsTrue(e.Success);
            Assert.IsTrue(File.Exists(e.ReplayBlob + ".done"));
            mre.Set();
        };

        var result = uploadService.Upload(uploadDto).GetAwaiter().GetResult();

        Assert.IsTrue(result);

        mre.WaitOne(10000);

        var count = context.Replays.Count();
        Assert.AreEqual(replayCount, count);
    }

    [TestMethod]
    public void T02ParallelUploadTest()
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        var uploadService = scope.ServiceProvider.GetRequiredService<UploadService>();

        int countBefore = context.Replays.Count();

        using var md5 = MD5.Create();

        int threads = 10;

        List<UploadDto> uploadDtos = new();

        for (int i = 0; i < threads; i++)
        {
            var replayDto = GetBasicReplayDto(md5);
            var jsonString = JsonSerializer.Serialize(new List<ReplayDto>() { replayDto });
            var base64string = UploadService.ZipAsync(jsonString).GetAwaiter().GetResult();

            var uploaderPlayer = replayDto.ReplayPlayers.First().Player;

            UploadDto uploadDto = new()
            {
                AppGuid = Guid.Empty,
                Base64ReplayBlob = base64string,
                RequestNames = new List<RequestNames>()
            {
                new(uploaderPlayer.Name, uploaderPlayer.ToonId, uploaderPlayer.RegionId, uploaderPlayer.RealmId)
            }
            };
            uploadDtos.Add(uploadDto);
        }

        ManualResetEvent mre = new ManualResetEvent(false);
        int t = 0;

        uploadService.BlobImported += (o, e) =>
        {
            Assert.IsTrue(e.Success);
            Assert.IsTrue(File.Exists(e.ReplayBlob + ".done"));
            Interlocked.Increment(ref t);
            if (t >= threads)
            {
                mre.Set();
            }
        };

        List<Task> tasks = new();

        foreach (var uploadDto in  uploadDtos)
        {
            var task = uploadService.Upload(uploadDto);
            tasks.Add(task);
        }

        Task.WaitAll(tasks.ToArray());
        var waitResult = mre.WaitOne(20000);
        Assert.IsTrue(waitResult);

        int countAfter = context.Replays.Count();
        Assert.AreEqual(countBefore + threads, countAfter);
    }

    [Ignore]
    [TestMethod]
    public void T03MassiveUploadTest()
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        var uploadService = scope.ServiceProvider.GetRequiredService<UploadService>();

        int countBefore = context.Replays.Count();

        using var md5 = MD5.Create();

        int threads = 111;

        List<UploadDto> uploadDtos = new();

        for (int i = 0; i < threads; i++)
        {
            List<ReplayDto> replays = new();
            for (int j = 0; j < 10; j++)
            {
                replays.Add(GetBasicReplayDto(md5));
            }
            var jsonString = JsonSerializer.Serialize(replays);
            var base64string = UploadService.ZipAsync(jsonString).GetAwaiter().GetResult();

            var uploaderPlayer = replays[0].ReplayPlayers.First().Player;

            UploadDto uploadDto = new()
            {
                AppGuid = Guid.Empty,
                Base64ReplayBlob = base64string,
                RequestNames = new List<RequestNames>()
            {
                new(uploaderPlayer.Name, uploaderPlayer.ToonId, uploaderPlayer.RegionId, uploaderPlayer.RealmId)
            }
            };
            uploadDtos.Add(uploadDto);
        }

        ManualResetEvent mre = new ManualResetEvent(false);
        int t = 0;
        int a = 0;

        uploadService.BlobImported += (o, e) =>
        {
            Interlocked.Increment(ref t);
            if (e.ReplayBlob == "too_many_uploads")
            {
                Interlocked.Increment(ref a);
            }
            else
            {
                Assert.IsTrue(e.Success);
                Assert.IsTrue(File.Exists(e.ReplayBlob + ".done"));
            }
            if (t >= threads)
            {
                mre.Set();
            }
        };

        List<Task> tasks = new();

        foreach (var uploadDto in uploadDtos)
        {
            var task = uploadService.Upload(uploadDto);
            tasks.Add(task);
        }

        var waitResult = mre.WaitOne(30000);
        Assert.IsTrue(a > 0);
        Assert.IsTrue(waitResult);

        int countAfter = context.Replays.Count();
        Assert.AreEqual(countBefore + threads, countAfter);
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
            ReplayPlayers = GetBasicReplayPlayerDtos().ToList()
        };
        replay.GenHash(md5);
        return replay;
    }

    public ReplayPlayerDto[] GetBasicReplayPlayerDtos()
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
