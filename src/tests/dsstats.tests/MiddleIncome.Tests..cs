using dsstats.shared;

namespace dsstats.tests;

[TestClass]
public class MiddleIncome
{
    private static ReplayDto GetTestReplay(int duration)
    {
        return new ReplayDto
        {
            Duration = duration,
        };
    }

    private static ReplayDto Replay(int duration, params int[] middleChanges)
    {
        return new ReplayDto
        {
            Duration = duration,
            MiddleChanges = new List<int>(middleChanges)
        };
    }

    private static MiddleControlHelper Helper(ReplayDto r)
        => new MiddleControlHelper(r);

    private static (double, double) P(MiddleControlHelper h, int second)
        => h.GetPercent(second);

    [TestMethod]
    public void CanGetResultWithEmptyMiddle()
    {
        var durationInSeconds = 600;
        var replay = GetTestReplay(durationInSeconds);
        (var team1Middle, var team2Middle) = replay.GetMiddleIncome(durationInSeconds);
        Assert.AreEqual(0, team1Middle);
        Assert.AreEqual(0, team2Middle);
    }

    [TestMethod]
    public void CanGetResultWithOneTeamOnlyHavingMiddle()
    {
        var durationInSeconds = 600;
        var replay = GetTestReplay(durationInSeconds);

        var firstMiddleTeam = 1;
        var firstCrossingMiddle = 100;
        replay.MiddleChanges = [firstMiddleTeam, firstCrossingMiddle];

        (var team1Middle, var team2Middle) = replay.GetMiddleIncome(durationInSeconds);

        var expectedTeam1Middle = durationInSeconds - firstCrossingMiddle;

        Assert.AreEqual(expectedTeam1Middle, team1Middle);
        Assert.AreEqual(0, team2Middle);
    }

    [TestMethod]
    public void CanGetResultWithOneTeamOnlyHavingMiddleAtBreakpoint()
    {
        var durationInSeconds = 600;
        var breakpoint = 300;
        var replay = GetTestReplay(durationInSeconds);

        var firstMiddleTeam = 1;
        var firstCrossingMiddle = 100;
        replay.MiddleChanges = [firstMiddleTeam, firstCrossingMiddle];

        (var team1Middle, var team2Middle) = replay.GetMiddleIncome(breakpoint);

        var expectedTeam1Middle = breakpoint - firstCrossingMiddle;

        Assert.AreEqual(expectedTeam1Middle, team1Middle);
        Assert.AreEqual(0, team2Middle);
    }

    [TestMethod]
    public void CanGetResultWithBothTeams1()
    {
        var durationInSeconds = 600;
        var replay = GetTestReplay(durationInSeconds);

        var firstMiddleTeam = 1;
        var firstTeamCrossingMiddle = 100;
        var secondTeamCrossingMiddle = 200;
        replay.MiddleChanges = [firstMiddleTeam, firstTeamCrossingMiddle, secondTeamCrossingMiddle];

        (var team1Middle, var team2Middle) = replay.GetMiddleIncome(durationInSeconds);

        var expectedTeam1 = secondTeamCrossingMiddle - firstTeamCrossingMiddle;
        var expectedTeam2 = durationInSeconds - secondTeamCrossingMiddle;

        Assert.AreEqual(expectedTeam1, team1Middle);
        Assert.AreEqual(expectedTeam2, team2Middle);
    }

    [TestMethod]
    public void CanGetResultWithBothTeams2()
    {
        var durationInSeconds = 600;
        var replay = GetTestReplay(durationInSeconds);

        var firstMiddleTeam = 1;
        var firstTeamCrossingMiddle1 = 100;
        var secondTeamCrossingMiddle = 200;
        var firstTeamCrossingMiddle2 = 300;
        replay.MiddleChanges = [firstMiddleTeam, firstTeamCrossingMiddle1, secondTeamCrossingMiddle, firstTeamCrossingMiddle2];

        (var team1Middle, var team2Middle) = replay.GetMiddleIncome(durationInSeconds);

        var expectedTeam1 = (secondTeamCrossingMiddle - firstTeamCrossingMiddle1) + (durationInSeconds - firstTeamCrossingMiddle2);
        var expectedTeam2 = firstTeamCrossingMiddle2 - secondTeamCrossingMiddle;

        Assert.AreEqual(expectedTeam1, team1Middle);
        Assert.AreEqual(expectedTeam2, team2Middle);
    }

    [TestMethod]
    public void CanGetResultWithBothTeamsAtBreakpoint()
    {
        var durationInSeconds = 600;
        var replay = GetTestReplay(durationInSeconds);

        var breakpoint = 250;
        var firstMiddleTeam = 1;
        var firstTeamCrossingMiddle1 = 100;
        var secondTeamCrossingMiddle = 200;
        var firstTeamCrossingMiddle2 = 300;
        replay.MiddleChanges = [firstMiddleTeam, firstTeamCrossingMiddle1, secondTeamCrossingMiddle, firstTeamCrossingMiddle2];

        (var team1Middle, var team2Middle) = replay.GetMiddleIncome(breakpoint);

        var expectedTeam1 = secondTeamCrossingMiddle - firstTeamCrossingMiddle1;
        var expectedTeam2 = breakpoint - secondTeamCrossingMiddle;

        Assert.AreEqual(expectedTeam1, team1Middle);
        Assert.AreEqual(expectedTeam2, team2Middle);
    }

    [TestMethod]
    public void NoMiddleControl_NoChanges_AllZero()
    {
        var replay = Replay(120);
        var h = Helper(replay);

        Assert.AreEqual((0, 0), P(h, 0));
        Assert.AreEqual((0, 0), P(h, 119));
        Assert.AreEqual((0, 0), P(h, 120));
    }

    [TestMethod]
    public void FirstTeamControlsEntireMatch_NoSwitch()
    {
        // Team 1 gains control at second 10 and keeps it.
        var replay = Replay(100, 1, 10);
        var h = Helper(replay);

        // Before t=10 → no control
        Assert.AreEqual((0, 0), P(h, 0));
        Assert.AreEqual((0, 0), P(h, 9));

        // After t=10 → team 1 gains time
        var p50 = P(h, 50);
        Assert.IsGreaterThan(0, p50.Item1);
        Assert.AreEqual(0, p50.Item2);

        // Final should be (90% team1, 0% team2)
        Assert.AreEqual((91, 0), P(h, 100));
    }

    [TestMethod]
    public void OneSwitch_Team1ThenTeam2()
    {
        // t=10 team1 takes mid
        // t=40 team2 takes mid
        // duration = 100
        var replay = Replay(100, 1, 10, 40);
        var h = Helper(replay);

        // Team1 holds from 10 → 39 → 30 seconds
        // Team2 holds from 40 → 100 → 60 seconds

        Assert.AreEqual((30, 61), (
            h.GetPercent(100).Item1,
            h.GetPercent(100).Item2
        ).Round());

        // Mid-match check
        Assert.AreEqual((30, 11), P(h, 50).Round());  // ~40% for team2 at t=50
    }

    [TestMethod]
    public void MultipleSwitches_CorrectAccumulation()
    {
        // Timeline:
        // 1 @ 10
        // switch @ 20 (team2)
        // switch @ 35 (team1)
        // switch @ 80 (team2)
        var replay = Replay(100, 1, 10, 20, 35, 80);
        var h = Helper(replay);

        // Breakdown:
        // 10–19 = team1 (10s)
        // 20–34 = team2 (15s)
        // 35–79 = team1 (45s)
        // 80–100 = team2 (20s)
        //
        // team1 = 10 + 45 = 55
        // team2 = 15 + 20 = 35

        var final = P(h, 100);

        Assert.AreEqual((55, 36), final.Round());
    }

    [TestMethod]
    public void QueryBeyondDuration_ClampsCorrectly()
    {
        var replay = Replay(50, 1, 0);
        var h = Helper(replay);

        var p = P(h, 999);

        // Team1 controls 100% of 50 seconds
        Assert.AreEqual((102, 0), p);
    }
}

public static class TupleExtensions
{
    public static (int, int) Round(this (double, double) p)
        => ((int)Math.Round(p.Item1), (int)Math.Round(p.Item2));
}