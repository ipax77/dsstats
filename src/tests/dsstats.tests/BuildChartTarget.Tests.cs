using dsstats.shared;
using dsstats.weblib.Replays;

namespace dsstats.tests;

[TestClass]
public sealed class BuildChartTargetTests
{
    [TestMethod]
    public void GetVisibleKey_ReturnsNullForClosedMap()
    {
        var key = BuildChartTarget.GetVisibleKey(false, "replay-1", 1, Breakpoint.Min5);

        Assert.IsNull(key);
    }

    [TestMethod]
    public void GetVisibleKey_ChangesWhenBreakpointChanges()
    {
        var min5 = BuildChartTarget.GetVisibleKey(true, "replay-1", 1, Breakpoint.Min5);
        var min10 = BuildChartTarget.GetVisibleKey(true, "replay-1", 1, Breakpoint.Min10);

        Assert.IsNotNull(min5);
        Assert.IsNotNull(min10);
        Assert.AreNotEqual(min5, min10);
    }

    [TestMethod]
    public void GetWaveKey_DoesNotCollideWithBreakpointKey()
    {
        var breakpoint = BuildChartTarget.GetKey("replay-1", 1, Breakpoint.Min5);
        var wave = BuildChartTarget.GetWaveKey("replay-1", 1, (int)Breakpoint.Min5, 6_720);

        Assert.AreNotEqual(breakpoint, wave);
    }

    [TestMethod]
    public void GetVisibleWaveKey_ReturnsNullForClosedMap()
    {
        var key = BuildChartTarget.GetVisibleWaveKey(false, "replay-1", 1, 3, 6_720);

        Assert.IsNull(key);
    }
}
