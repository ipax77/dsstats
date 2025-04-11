
using System.Security.Cryptography;
using dsstats.db.Services.Import;
using dsstats.shared;
using dsstats.shared.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace dsstats.db.Tests;

[TestClass]
public class ImportTests
{
    private readonly ServiceProvider serviceProvider;

    public ImportTests()
    {
        var services = TestServiceCollection.GetServiceCollection();
        services.AddSingleton<ImportService>();
        serviceProvider = services.BuildServiceProvider();
    }

    [TestMethod]
    public async Task T01BasicImportTest()
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();
        var importService = scope.ServiceProvider.GetRequiredService<ImportService>();

        context.Database.EnsureDeleted();
        context.Database.Migrate();
        using var md5 = MD5.Create();
        var replayDto = GetBasicReplayDto(md5);

        await importService.Import([replayDto]);

        var replays = context.Replays.ToList();

        Assert.IsTrue(replays.Count > 0);
    }

    [TestMethod]
    public async Task T02BasicDuplicateTest()
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();
        var importService = scope.ServiceProvider.GetRequiredService<ImportService>();

        context.Database.EnsureDeleted();
        context.Database.Migrate();
        using var md5 = MD5.Create();
        var replayDto = GetBasicReplayDto(md5);

        await importService.Import([replayDto]);

        var replays = context.Replays.ToList();
        var replaysBefore = replays.Count;

        await importService.Import([replayDto]);

        replays = context.Replays.ToList();
        var replaysAfter = replays.Count;

        Assert.AreEqual(replaysBefore, replaysAfter);
    }

    [TestMethod]
    public async Task T03AdvancedDuplicateTest()
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();
        var importService = scope.ServiceProvider.GetRequiredService<ImportService>();

        context.Database.EnsureDeleted();
        context.Database.Migrate();
        using var md5 = MD5.Create();
        var replayDto = GetBasicReplayDto(md5);

        await importService.Import([replayDto]);

        var replay = context.Replays.FirstOrDefault(f => f.ReplayHash == replayDto.ReplayHash);
        Assert.IsNotNull(replay);
        var gameTimeBefore = replay.GameTime;
        var newGameTime = new DateTime(gameTimeBefore.Year, gameTimeBefore.Month, gameTimeBefore.Day, gameTimeBefore.Hour, gameTimeBefore.Minute + 1, gameTimeBefore.Second);

        await importService.Import([replayDto with { GameTime = newGameTime, Duration = replayDto.Duration + 1 }]);

        replay = context.Replays.FirstOrDefault(f => f.ReplayHash == replayDto.ReplayHash);
        Assert.IsNotNull(replay);
        var gameTimeAfter = replay.GameTime;

        Assert.AreEqual(newGameTime, gameTimeAfter);
    }

    [TestMethod]
    public async Task T04LastSpawnDuplicateTest()
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();
        var importService = scope.ServiceProvider.GetRequiredService<ImportService>();

        context.Database.EnsureDeleted();
        context.Database.Migrate();
        using var md5 = MD5.Create();
        var replayDto = GetBasicReplayDto(md5);
        var replayPlayer = replayDto.ReplayPlayers.First(f => f.GamePos == 1);
        replayPlayer.Spawns.Add(CreateRandomSpawn());

        await importService.Import([replayDto]);

        var replay = context.Replays.FirstOrDefault(f => f.ReplayHash == replayDto.ReplayHash);
        Assert.IsNotNull(replay);
        var gameTimeBefore = replay.GameTime;
        var newGameTime = new DateTime(gameTimeBefore.Year, gameTimeBefore.Month, gameTimeBefore.Day, gameTimeBefore.Hour, gameTimeBefore.Minute, gameTimeBefore.Second)
            .AddMinutes(5);
        var countBefore = context.Replays.Count();

        replayDto = replayDto with
        {
            GameTime = newGameTime,
            Duration = replayDto.Duration + 200,
            Maxkillsum = replayDto.Maxkillsum + 500,
        };
        replayDto.GenHash(md5);
        await importService.Import([replayDto]);

        replay = context.Replays.FirstOrDefault(f => f.ReplayHash == replayDto.ReplayHash);
        Assert.IsNotNull(replay);
        var gameTimeAfter = replay.GameTime;
        var countAfter = context.Replays.Count();

        Assert.AreEqual(newGameTime, gameTimeAfter);
        Assert.AreEqual(countBefore, countAfter);
    }

    [TestMethod]
    public async Task T05CanImportArcadeReplays()
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();
        var importService = scope.ServiceProvider.GetRequiredService<ImportService>();
        
        context.Database.EnsureDeleted();
        context.Database.Migrate();

        var arcadeReplayDto = GetArcadeReplayDto();
        await importService.ImportArcadeReplays([arcadeReplayDto]);

        var arcadeReplays = context.ArcadeReplays.Count();
        Assert.IsTrue(arcadeReplays > 0);
    }

    public static ArcadeReplayDto GetArcadeReplayDto(ReplayDto? replayDto = null)
    {
        var regionId = replayDto?.ReplayPlayers.First().Player.RegionId ?? Random.Shared.Next(1, 4);
        var arcadeReplay = new ArcadeReplayDto()
        {
            CreatedAt = replayDto == null ? DateTime.UtcNow : replayDto.GameTime,
            GameMode = replayDto == null ? GameMode.Commanders : replayDto.GameMode,
            RegionId = regionId,
            BnetBucketId = Random.Shared.Next(1_000, 1_000_000),
            BnetRecordId = Random.Shared.Next(1_000, 1_000_000),
            WinnerTeam = replayDto == null ? 1 : replayDto.WinnerTeam,
            Duration = replayDto == null ? 500 : replayDto.Duration,
            ArcadeReplayDsPlayers = GetArcadeReplayPlayers(replayDto)
        };
        return arcadeReplay;
    }

    private static List<ArcadeReplayDsPlayerDto> GetArcadeReplayPlayers(ReplayDto? replayDto)
    {
        var players = GetDefaultPlayers(replayDto);
        return players.Select((s, i) => new ArcadeReplayDsPlayerDto()
        {
            Name = "Test",
            SlotNumber = i + 1,
            Team = i + 1 <= 3 ? 1 : 2,
            PlayerResult = i + 1 <= 3 ? PlayerResult.Win : PlayerResult.Los,
            Player = s,
        }).ToList();
    }

    public static SpawnDto CreateRandomSpawn()
    {
        SpawnDto spawn = new()
        {
            Breakpoint = Breakpoint.All,
            ArmyValue = Random.Shared.Next(100, 1000),
            KilledValue = Random.Shared.Next(100, 1000),
            Income = Random.Shared.Next(100, 1000),
            UpgradeSpent = Random.Shared.Next(100, 1000),
            GasCount = Random.Shared.Next(0, 3),
            Gameloop = 400,
            Units = [new SpawnUnitDto() { Count = 1, Poss = "20,30", Unit = new UnitDto() { Name = "Kerrigan " } }]
        };

        return spawn;
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
            CommandersTeam1 = gameMode == GameMode.Standard ? "|1|1|1|" : "|10|10|10|",
            CommandersTeam2 = "|10|10|10|",
            Playercount = 6,
            Middle = "",
            ReplayPlayers = GetBasicReplayPlayerDtos(gameMode).ToList()
        };
        replay.GenHash(md5);
        return replay;
    }

    public static ReplayPlayerDto[] GetBasicReplayPlayerDtos(GameMode gameMode)
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
        }).ToArray();
    }

    public static PlayerDto[] GetDefaultPlayers(ReplayDto? replayDto = null)
    {
        if (replayDto is null)
        {
            var regionId = Random.Shared.Next(1, 4);
            List<PlayerDto> playerDtos = [];
            for (int i = 0; i < 6; i++)
            {
                playerDtos.Add(new()
                {
                    Name = "Test",
                    RegionId = regionId,
                    RealmId = Random.Shared.Next(1, 3),
                    ToonId = Random.Shared.Next(1, 100_000)
                });
            }
            return playerDtos.ToArray();
        }
        else
        {
            return replayDto.ReplayPlayers
                .OrderBy(o => o.GamePos)
                .Select(s => s.Player)
                .ToArray();
        }
    }
}