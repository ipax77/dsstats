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
}
