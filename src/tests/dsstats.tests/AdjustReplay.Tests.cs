using dsstats.dbServices;
using dsstats.shared;

namespace dsstats.tests;


[TestClass]
public class AdjustReplayTests
{
    public static ReplayDto GetTestReplay()
    {
        return new ReplayDto
        {
            Gametime = DateTime.UtcNow,
            WinnerTeam = 0,
            Duration = 600,
            Cannon = 0,
            Bunker = 0,
            MiddleChanges = [],
            Players =
            [
                new ReplayPlayerDto
                {
                    TeamId = 1,
                    Spawns =
                    [
                        new SpawnDto { Breakpoint = Breakpoint.All, Income = 100, KilledValue = 100 },
                        new SpawnDto { Breakpoint = Breakpoint.All, Income = 100, KilledValue = 100 }
                    ]
                },
                new ReplayPlayerDto
                {
                    TeamId = 1,
                    Spawns =
                    [
                        new SpawnDto { Breakpoint = Breakpoint.All, Income = 100, KilledValue = 100 },
                        new SpawnDto { Breakpoint = Breakpoint.All, Income = 100, KilledValue = 100 }
                    ]
                },
                new ReplayPlayerDto
                {
                    TeamId = 1,
                    Spawns =
                    [
                        new SpawnDto { Breakpoint = Breakpoint.All, Income = 100, KilledValue = 100 },
                        new SpawnDto { Breakpoint = Breakpoint.All, Income = 100, KilledValue = 100 }
                    ]
                },
                new ReplayPlayerDto
                {
                    TeamId = 2,
                    Spawns =
                    [
                        new SpawnDto { Breakpoint = Breakpoint.All, Income = 100, KilledValue = 100 },
                        new SpawnDto { Breakpoint = Breakpoint.All, Income = 100, KilledValue = 100 }
                    ]
                },
                                new ReplayPlayerDto
                {
                    TeamId = 2,
                    Spawns =
                    [
                        new SpawnDto { Breakpoint = Breakpoint.All, Income = 100, KilledValue = 100 },
                        new SpawnDto { Breakpoint = Breakpoint.All, Income = 100, KilledValue = 100 }
                    ]
                },
                                new ReplayPlayerDto
                {
                    TeamId = 2,
                    Spawns =
                    [
                        new SpawnDto { Breakpoint = Breakpoint.All, Income = 100, KilledValue = 100 },
                        new SpawnDto { Breakpoint = Breakpoint.All, Income = 100, KilledValue = 100 }
                    ]
                }
            ]
        };
    }

    public static void SetIncome(ReplayDto replay, int team1Income, int team2Income)
    {
        foreach (var player in replay.Players)
        {
            foreach (var spawn in player.Spawns)
            {
                if (player.TeamId == 1)
                {
                    spawn.Income = team1Income / replay.Players.Count(p => p.TeamId == 1);
                }
                else
                {
                    spawn.Income = team2Income / replay.Players.Count(p => p.TeamId == 2);
                }
            }
        }
    }

    public static void SetKills(ReplayDto replay, int team1Kills, int team2Kills)
    {
        foreach (var player in replay.Players)
        {
            foreach (var spawn in player.Spawns)
            {
                if (player.TeamId == 1)
                {
                    spawn.KilledValue = team1Kills / replay.Players.Count(p => p.TeamId == 1);
                }
                else
                {
                    spawn.KilledValue = team2Kills / replay.Players.Count(p => p.TeamId == 2);
                }
            }
        }
    }


    [TestMethod]
    public void TestAdjustReplay_NoAdjustments()
    {
        var replay = GetTestReplay();
        ImportService.AdjustReplayResult(replay);
        Assert.AreEqual(0, replay.WinnerTeam);
    }

    [TestMethod]
    public void TestAdjustReplay_NoAdjustments2()
    {
        var replay = GetTestReplay();
        replay.Cannon = 1;
        replay.Bunker = 1;
        SetIncome(replay, 1000, 2000);
        SetKills(replay, 2000, 1000);
        ImportService.AdjustReplayResult(replay);
        Assert.AreEqual(0, replay.WinnerTeam);
    }

    [TestMethod]
    public void TestAdjustReplay_Borderline_NoWinner()
    {
        var replay = GetTestReplay();
        SetIncome(replay, 1200, 1000); // small advantage
        SetKills(replay, 1100, 1000);  // small advantage
        ImportService.AdjustReplayResult(replay);

        Assert.AreEqual(0, replay.WinnerTeam);
    }

    [TestMethod]
    public void TestAdjustReplay_ObjectiveOnly_NotEnough()
    {
        var replay = GetTestReplay();
        replay.Cannon = 1; // objective only
        ImportService.AdjustReplayResult(replay);

        Assert.AreEqual(0, replay.WinnerTeam);
    }

    [TestMethod]
    public void TestAdjustReplay_VictoryUpgrade()
    {
        var replay = GetTestReplay();
        replay.Players.First(f => f.TeamId == 1).Upgrades.Add(new() { Name = "PlayerStateVictory" });
        ImportService.AdjustReplayResult(replay);
        Assert.AreEqual(1, replay.WinnerTeam);
    }

    [TestMethod]
    public void TestAdjustReplay_Objectives()
    {
        var replay = GetTestReplay();
        replay.Cannon = 1; // Team 1 destroyed objective
        SetIncome(replay, 2000, 1000);
        ImportService.AdjustReplayResult(replay);
        Assert.AreEqual(1, replay.WinnerTeam);
    }

    [TestMethod]
    public void TestAdjustReplay_Middle()
    {
        var replay = GetTestReplay();
        replay.Cannon = 1; // Team 1 destroyed objective
        replay.MiddleChanges = [1, 1];
        ImportService.AdjustReplayResult(replay);
        Assert.AreEqual(1, replay.WinnerTeam);
    }

    [TestMethod]
    public void TestAdjustReplay_Kills()
    {
        var replay = GetTestReplay();
        SetKills(replay, 1000, 2000);
        replay.MiddleChanges = [1, 1, 2];
        ImportService.AdjustReplayResult(replay);
        Assert.AreEqual(2, replay.WinnerTeam);
    }
}
