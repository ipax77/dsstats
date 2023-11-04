
using dsstats.db8services.Import;
using dsstats.shared;
using dsstats.shared.Extensions;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;

namespace dsstats.ratings.tests;

[TestClass]
public class AdjustTests
{
    private readonly string assemblyPath;
    private readonly List<RequestNames> playerPool;
    private readonly List<UpgradeDto> upgradePool;
    private readonly List<UnitDto> unitPool;
    private readonly int poolCount = 100;

    public AdjustTests()
    {
        assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";

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
    }


    [TestMethod]
    [DataRow("adjustTestTeam1_0.json")]
    [DataRow("adjustTestTeam1_1.json")]
    [DataRow("adjustTestTeam1_2.json")]
    [DataRow("adjustTestTeam1_3.json")]
    [DataRow("adjustTestTeam1_4.json")]
    public void AdjustReplayWinnerTeam1Test(string testFile)
    {
        var replay = GetReplayDto(testFile);

        Assert.IsTrue(replay.WinnerTeam == 0);

        ImportService.AdjustReplay(replay);

        Assert.AreEqual(1, replay.WinnerTeam);
    }

    [TestMethod]
    [DataRow("adjustTestTeam2_0.json")]
    [DataRow("adjustTestTeam2_1.json")]
    [DataRow("adjustTestTeam2_2.json")]
    [DataRow("adjustTestTeam2_3.json")]
    [DataRow("adjustTestTeam2_4.json")]
    public void AdjustReplayWinnerTeam2Test(string testFile)
    {
        var replay = GetReplayDto(testFile);

        Assert.IsTrue(replay.WinnerTeam == 0);

        ImportService.AdjustReplay(replay);

        Assert.AreEqual(2, replay.WinnerTeam);
    }

    [TestMethod]
    public void AdjustReplayByUpgradesTest()
    {
        using var md5 = MD5.Create();
        var replay = GetBasicReplayDto(md5);
        int gameloopEnd = Convert.ToInt32(replay.Duration * 22.4 - 4);

        replay.WinnerTeam = 0;
        foreach (var rp in replay.ReplayPlayers)
        {
            rp.PlayerResult = PlayerResult.None;
            rp.Upgrades.Add(new() { Gameloop = gameloopEnd, Upgrade = new() { Name = "PlayerStateGameOver" } });
            if (rp.Team == 1)
            {
                rp.Upgrades.Add(new() { Gameloop = gameloopEnd, Upgrade = new() { Name = "PlayerStateVictory" } });
            }
        }

        ImportService.AdjustReplay(replay);

        Assert.AreEqual(1, replay.WinnerTeam);
    }

    private ReplayDto GetReplayDto(string file)
    {
        var path = Path.Combine(assemblyPath, "testdata", file);
        
        var replay = JsonSerializer.Deserialize<ReplayDto>(File.ReadAllText(path));
        ArgumentNullException.ThrowIfNull(replay);
        return replay;
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
