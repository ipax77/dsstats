using dsstats.parser;
using dsstats.shared;

namespace dsstats.tests;

[TestClass]
public sealed class GetIncomeTests
{
    // min5 = 6720 gameloops => 6720/22.4 = 300 seconds
    // baseIncome: 300 * 7.5 = 2250
    // baseGasIncome per second: 0.5
    // refineryCosts: [150, 225, 300, 375, 500]

    private static DsstatsReplay MakeReplay(int duration = 27000) => new()
    {
        Duration = duration
    };

    private static DsPlayer MakePlayer(int teamId = 1) => new()
    {
        TeamId = teamId,
        ToonId = new ToonId { Region = 1, Realm = 1, Id = 1 }
    };

    [TestMethod]
    public void BaseIncomeOnly_NoRefineries_NoMiddle()
    {
        var replay = MakeReplay();
        var player = MakePlayer();

        int income = DsstatsReplayMapper.GetIncome(replay, player, Breakpoint.Min5);

        // (6720 / 22.4) * 7.5 = 300 * 7.5 = 2250
        Assert.AreEqual(2250, income);
    }

    [TestMethod]
    public void BaseIncomeOnly_AtMin10()
    {
        var replay = MakeReplay();
        var player = MakePlayer();

        int income = DsstatsReplayMapper.GetIncome(replay, player, Breakpoint.Min10);

        // (13440 / 22.4) * 7.5 = 600 * 7.5 = 4500
        Assert.AreEqual(4500, income);
    }

    [TestMethod]
    public void SingleRefinery_TakenAtGameloopZero_BreaksEven()
    {
        var replay = MakeReplay();
        var player = MakePlayer();
        player.Refineries.Add(new Refinery { Gameloop = 0, Taken = true });

        int income = DsstatsReplayMapper.GetIncome(replay, player, Breakpoint.Min5);

        // gasIncome = (6720/22.4)*0.5 - 150 = 150 - 150 = 0
        // total = 2250 + 0 = 2250
        Assert.AreEqual(2250, income);
    }

    [TestMethod]
    public void SingleRefinery_TakenMidGame_ReducesIncome()
    {
        var replay = MakeReplay();
        var player = MakePlayer();
        // Taken at halfway point of min5 window
        player.Refineries.Add(new Refinery { Gameloop = 3360, Taken = true });

        int income = DsstatsReplayMapper.GetIncome(replay, player, Breakpoint.Min5);

        // gasIncome = ((6720 - 3360)/22.4)*0.5 - 150 = 75 - 150 = -75
        // total = 2250 - 75 = 2175
        Assert.AreEqual(2175, income);
    }

    [TestMethod]
    public void TwoRefineries_BothFromStart_SecondCostsMore()
    {
        var replay = MakeReplay();
        var player = MakePlayer();
        player.Refineries.Add(new Refinery { Gameloop = 0, Taken = true });
        player.Refineries.Add(new Refinery { Gameloop = 0, Taken = true });

        int income = DsstatsReplayMapper.GetIncome(replay, player, Breakpoint.Min5);

        // ref1: 150 - 150 = 0
        // ref2: 150 - 225 = -75
        // total = 2250 - 75 = 2175
        Assert.AreEqual(2175, income);
    }

    [TestMethod]
    public void UntakenRefineries_AreIgnored()
    {
        var replay = MakeReplay();
        var player = MakePlayer();
        player.Refineries.Add(new Refinery { Gameloop = 0, Taken = false });
        player.Refineries.Add(new Refinery { Gameloop = 0, Taken = false });

        int income = DsstatsReplayMapper.GetIncome(replay, player, Breakpoint.Min5);

        Assert.AreEqual(2250, income);
    }

    [TestMethod]
    public void MiddleIncome_Team1GetsTeam1Value()
    {
        var replay = MakeReplay();
        replay.MiddleIncome[Breakpoint.Min5] = new dsstats.parser.MiddleIncome { Team1 = 500, Team2 = 0 };
        var player = MakePlayer(teamId: 1);

        int income = DsstatsReplayMapper.GetIncome(replay, player, Breakpoint.Min5);

        // 2250 (base) + 500 (middle) = 2750
        Assert.AreEqual(2750, income);
    }

    [TestMethod]
    public void MiddleIncome_Team2GetsTeam2Value()
    {
        var replay = MakeReplay();
        replay.MiddleIncome[Breakpoint.Min5] = new dsstats.parser.MiddleIncome { Team1 = 0, Team2 = 400 };
        var player = MakePlayer(teamId: 2);

        int income = DsstatsReplayMapper.GetIncome(replay, player, Breakpoint.Min5);

        // 2250 (base) + 400 (middle) = 2650
        Assert.AreEqual(2650, income);
    }

    [TestMethod]
    public void SameRefineries_MoreMiddleControlMeansHigherIncome()
    {
        var replay = MakeReplay();
        replay.MiddleIncome[Breakpoint.Min5] = new dsstats.parser.MiddleIncome { Team1 = 300, Team2 = 0 };

        var playerWithMiddle = MakePlayer(teamId: 1);
        playerWithMiddle.Refineries.Add(new Refinery { Gameloop = 0, Taken = true });

        var playerWithoutMiddle = MakePlayer(teamId: 2);
        playerWithoutMiddle.Refineries.Add(new Refinery { Gameloop = 0, Taken = true });

        int incomeWithMiddle = DsstatsReplayMapper.GetIncome(replay, playerWithMiddle, Breakpoint.Min5);
        int incomeWithoutMiddle = DsstatsReplayMapper.GetIncome(replay, playerWithoutMiddle, Breakpoint.Min5);

        Assert.IsGreaterThan(incomeWithoutMiddle, incomeWithMiddle,
            "Player with middle control should earn more than player without it");
    }

    [TestMethod]
    public void NoMiddleControlEntry_DoesNotCrash_ReturnsBaseIncome()
    {
        var replay = MakeReplay();
        // MiddleIncome dictionary is empty — no entry for Min5
        var player = MakePlayer();

        int income = DsstatsReplayMapper.GetIncome(replay, player, Breakpoint.Min5);

        Assert.AreEqual(2250, income);
    }

    [TestMethod]
    public void RefineryAfterBreakpoint_IsNotCounted()
    {
        var replay = MakeReplay();
        var player = MakePlayer();
        // Refinery taken AFTER the min5 gameloop — should be excluded
        player.Refineries.Add(new Refinery { Gameloop = 7000, Taken = true });

        int income = DsstatsReplayMapper.GetIncome(replay, player, Breakpoint.Min5);

        // Only base income counts — condition is refinery.Gameloop < gameloop (6720)
        Assert.AreEqual(2250, income);
    }
}

[TestClass]
public sealed class GetMiddleIncomeTests
{
    [TestMethod]
    public void Team1ThenTeam2Switch_CorrectAccumulation()
    {
        // Team1 takes middle at 2240 gl (100s), Team2 at 6720 gl (300s), game ends at 13440 gl (600s)
        // Expected: team1 = 200s, team2 = 300s
        var replay = new DsstatsReplay
        {
            Duration = 13440,
            MiddleChanges =
            [
                new DsMiddle { Gameloop = 2240, ControlTeam = 1 },
                new DsMiddle { Gameloop = 6720, ControlTeam = 2 },
            ]
        };

        var (team1, team2) = DsstatsParser.GetMiddleIncome(replay, 13440);

        Assert.AreEqual(200, team1, "Team1 should accumulate the 200s before Team2 took middle");
        Assert.AreEqual(300, team2, "Team2 should accumulate the 300s after they took middle");
    }

    [TestMethod]
    public void Team2ThenTeam1Switch_CorrectAccumulation()
    {
        // Team2 at 2240 gl (100s), Team1 at 6720 gl (300s), game ends at 13440 gl (600s)
        // Expected: team1 = 300s, team2 = 200s
        var replay = new DsstatsReplay
        {
            Duration = 13440,
            MiddleChanges =
            [
                new DsMiddle { Gameloop = 2240, ControlTeam = 2 },
                new DsMiddle { Gameloop = 6720, ControlTeam = 1 },
            ]
        };

        var (team1, team2) = DsstatsParser.GetMiddleIncome(replay, 13440);

        Assert.AreEqual(300, team1, "Team1 should accumulate after their takeover");
        Assert.AreEqual(200, team2, "Team2 should accumulate only before Team1 took middle");
    }

    [TestMethod]
    public void MultipleSwitches_CorrectAccumulation()
    {
        // Team1 at 2240 gl (100s), Team2 at 4480 gl (200s), Team1 at 6720 gl (300s), game ends at 13440 gl (600s)
        // Team1: 100s (100–200) + 300s (300–600) = 400s
        // Team2: 100s (200–300)
        var replay = new DsstatsReplay
        {
            Duration = 13440,
            MiddleChanges =
            [
                new DsMiddle { Gameloop = 2240, ControlTeam = 1 },
                new DsMiddle { Gameloop = 4480, ControlTeam = 2 },
                new DsMiddle { Gameloop = 6720, ControlTeam = 1 },
            ]
        };

        var (team1, team2) = DsstatsParser.GetMiddleIncome(replay, 13440);

        Assert.AreEqual(400, team1);
        Assert.AreEqual(100, team2);
    }

    [TestMethod]
    public void EarlyExit_AtBreakpointBeforeSecondSwitch_CorrectAccumulation()
    {
        // Team1 at 2240 gl (100s), Team2 at 6720 gl (300s) — but we query at 4480 gl (200s)
        // Team1 controlled 100s–200s = 100s, Team2 hasn't taken yet
        var replay = new DsstatsReplay
        {
            Duration = 13440,
            MiddleChanges =
            [
                new DsMiddle { Gameloop = 2240, ControlTeam = 1 },
                new DsMiddle { Gameloop = 6720, ControlTeam = 2 },
            ]
        };

        var (team1, team2) = DsstatsParser.GetMiddleIncome(replay, 4480);

        Assert.AreEqual(100, team1);
        Assert.AreEqual(0, team2);
    }
}
