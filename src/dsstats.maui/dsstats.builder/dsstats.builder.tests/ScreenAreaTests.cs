namespace dsstats.builder.tests;

[TestClass]
public sealed class ScreenAreaTests
{
    private void AssertPointsAreClose(RlPoint expected, RlPoint actual, int tolerance = 5)
    {
        Assert.IsTrue(
            Math.Abs(expected.X - actual.X) <= tolerance &&
            Math.Abs(expected.Y - actual.Y) <= tolerance,
            $"Expected ({expected.X}, {expected.Y}), but got ({actual.X}, {actual.Y}) with tolerance ±{tolerance}");
    }

    [TestMethod]
    public void CanCalculateCenter()
    {
        var screenArea = new ScreenArea(2560, 1440);
        var center = screenArea.GetCenter();
        var expected = new RlPoint(1294, 628); // Geometric center
        AssertPointsAreClose(expected, center);
    }

    [TestMethod]
    public void RespectesScreenResolution()
    {
        var screenArea1 = new ScreenArea(2560, 1440);
        var screenArea2 = new ScreenArea(1920, 1080);
        var center1 = screenArea1.GetCenter();
        var center2 = screenArea2.GetCenter();

        Assert.AreNotEqual(center1, center2);
    }

    // [TestMethod]
    // public void CanMapCenterPoint()
    // {
    //     var screenArea = new ScreenArea(2560, 1440);
    //     var buildArea = new BuildArea();
    //     var buildCenter = buildArea.GetCenter();
    //     var normalizedBuildCenter = buildArea.NormalizeToTop(buildCenter);
    //     var screenPos = screenArea.GetScreenPosition(normalizedBuildCenter);
    //     var expected = screenArea.GetCenter();
    //     AssertPointsAreClose(expected, screenPos);
    // }

    [TestMethod]
    public void CanMapLeftPoint()
    {
        var screenArea = new ScreenArea(2560, 1440);
        var replayPos = new RlPoint(73, 82);
        var buildArea = new BuildArea();
        var normalizedReplayPos = buildArea.NormalizeToTop(replayPos);
        var screenPos = screenArea.GetScreenPosition(normalizedReplayPos);

        var expected = new RlPoint(485, 437);
        AssertPointsAreClose(expected, screenPos);
    }

    [TestMethod]
    public void CanMapRightPoint()
    {
        var screenArea = new ScreenArea(2560, 1440);
        var replayPos = new RlPoint(101, 76);
        var buildArea = new BuildArea();
        var normalizedReplayPos = buildArea.NormalizeToTop(replayPos);
        var screenPos = screenArea.GetScreenPosition(normalizedReplayPos);

        var expected = new RlPoint(2100, 765);
        AssertPointsAreClose(expected, screenPos);
    }

    [TestMethod]
    public void CanMapTopPoint()
    {
        var screenArea = new ScreenArea(2560, 1440);
        var replayPos = new RlPoint(84, 93);
        var buildArea = new BuildArea();
        var normalizedReplayPos = buildArea.NormalizeToTop(replayPos);
        var screenPos = screenArea.GetScreenPosition(normalizedReplayPos);

        var expected = new RlPoint(1124, -110);
        AssertPointsAreClose(expected, screenPos);
    }

    [TestMethod]
    public void CanMapBottomPoint()
    {
        var screenArea = new ScreenArea(2560, 1440);
        var replayPos = new RlPoint(90, 65);
        var buildArea = new BuildArea();
        var normalizedReplayPos = buildArea.NormalizeToTop(replayPos);
        var screenPos = screenArea.GetScreenPosition(normalizedReplayPos);

        var expected = new RlPoint(1468, 1423);
        AssertPointsAreClose(expected, screenPos);
    }
}

