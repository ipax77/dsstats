using Sc2DirectStrike.Parser;

namespace Sc2DirectStrike.Tests;

[TestClass]
public sealed class MiddleControlHelperTests
{
    [TestMethod]
    public void NoMiddleControlReturnsZeroControlAndPercent()
    {
        MiddleControlHelper helper = new(CreateReplay(TimeSpan.FromMinutes(10), 0));

        (TimeSpan team1, TimeSpan team2) = helper.GetControl(TimeSpan.FromMinutes(5));
        (double team1Percent, double team2Percent) = helper.GetPercent(TimeSpan.FromMinutes(5));

        Assert.AreEqual(TimeSpan.Zero, team1);
        Assert.AreEqual(TimeSpan.Zero, team2);
        Assert.AreEqual(0, team1Percent);
        Assert.AreEqual(0, team2Percent);
    }

    [TestMethod]
    public void FirstTeamControlsFromFirstMiddleChange()
    {
        MiddleControlHelper helper = new(CreateReplay(
            TimeSpan.FromMinutes(10),
            1,
            TimeSpan.FromMinutes(1)));

        (TimeSpan team1, TimeSpan team2) = helper.GetControl(TimeSpan.FromMinutes(3));

        Assert.AreEqual(TimeSpan.FromMinutes(2), team1);
        Assert.AreEqual(TimeSpan.Zero, team2);
    }

    [TestMethod]
    public void MiddleControlSwitchesAlternateTeams()
    {
        MiddleControlHelper helper = new(CreateReplay(
            TimeSpan.FromMinutes(10),
            1,
            TimeSpan.FromMinutes(1),
            TimeSpan.FromMinutes(2),
            TimeSpan.FromMinutes(4)));

        (TimeSpan team1, TimeSpan team2) = helper.GetControl(TimeSpan.FromMinutes(5));

        Assert.AreEqual(TimeSpan.FromMinutes(2), team1);
        Assert.AreEqual(TimeSpan.FromMinutes(2), team2);
    }

    [TestMethod]
    public void PercentUsesElapsedTimeDenominator()
    {
        MiddleControlHelper helper = new(CreateReplay(
            TimeSpan.FromMinutes(10),
            1,
            TimeSpan.FromMinutes(1),
            TimeSpan.FromMinutes(2)));

        (double team1, double team2) = helper.GetPercent(TimeSpan.FromMinutes(4));

        Assert.AreEqual(25D, team1);
        Assert.AreEqual(50D, team2);
    }

    [TestMethod]
    public void QueryTimeIsClampedToReplayDuration()
    {
        MiddleControlHelper helper = new(CreateReplay(
            TimeSpan.FromMinutes(3),
            2,
            TimeSpan.FromMinutes(1)));

        (TimeSpan beforeTeam1, TimeSpan beforeTeam2) = helper.GetControl(TimeSpan.FromSeconds(-1));
        (TimeSpan afterTeam1, TimeSpan afterTeam2) = helper.GetControl(TimeSpan.FromMinutes(10));

        Assert.AreEqual(TimeSpan.Zero, beforeTeam1);
        Assert.AreEqual(TimeSpan.Zero, beforeTeam2);
        Assert.AreEqual(TimeSpan.Zero, afterTeam1);
        Assert.AreEqual(TimeSpan.FromMinutes(2), afterTeam2);
    }

    private static ReplayDto CreateReplay(TimeSpan duration, int firstTeamCrossedMiddle, params TimeSpan[] middleChanges)
    {
        return new()
        {
            FileName = "test.SC2Replay",
            CompatHash = string.Empty,
            Title = "Direct Strike",
            Version = "1.0",
            Duration = (int)duration.TotalSeconds,
            MiddleChanges =
            [
                firstTeamCrossedMiddle,
                .. middleChanges.Select(change => (int)change.TotalSeconds),
            ],
        };
    }
}
