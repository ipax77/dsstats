namespace dsstats.builder.tests;

[TestClass]
public sealed class ScreenAreaTests
{
    [TestMethod]
    public void CanCalculateCenter()
    {
        var screenArea = new ScreenArea(2560, 1440);
        var center = screenArea.GetCenter();
        var expected = new RlPoint(1280, 600);
        Assert.AreEqual(expected, center, $"Wrong center: ({center.X},{center.Y}");
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

    [TestMethod]
    public void CanMapCenterPoint()
    {
        var screenArea = new ScreenArea(2560, 1440);
        var buildArea = new BuildArea();
        var buildCenter = buildArea.GetCenter();
        var screenPos = screenArea.GetScreenPosition(buildCenter);
        var expected = screenArea.GetCenter();
        Assert.AreEqual(expected, screenPos, $"point map failed: ({expected.X},{expected.Y}) => ({screenPos.X},{screenPos.Y})");
    }

    [TestMethod]
    public void CanMapLeftPoint()
    {
        var screenArea = new ScreenArea(2560, 1440);
        var replayPos = new RlPoint(73, 82);
        var screenPos = screenArea.GetScreenPosition(replayPos);

        var expected = new RlPoint(485, 437);
        Assert.AreEqual(expected, screenPos, $"point map failed: ({expected.X},{expected.Y}) => ({screenPos.X},{screenPos.Y})");
    }

    [TestMethod]
    public void CanMapRightPoint()
    {
        var screenArea = new ScreenArea(2560, 1440);
        var replayPos = new RlPoint(101, 76);
        var screenPos = screenArea.GetScreenPosition(replayPos);

        var expected = new RlPoint(2100, 765);
        Assert.AreEqual(expected, screenPos, $"point map failed: ({expected.X},{expected.Y}) => ({screenPos.X},{screenPos.Y})");
    }

    [TestMethod]
    public void CanMapTopPoint()
    {
        var screenArea = new ScreenArea(2560, 1440);
        var replayPos = new RlPoint(84, 93);
        var screenPos = screenArea.GetScreenPosition(replayPos);

        var expected = new RlPoint(1124, -110);
        Assert.AreEqual(expected, screenPos, $"point map failed: ({expected.X},{expected.Y}) => ({screenPos.X},{screenPos.Y})");
    }

    [TestMethod]
    public void CanMapBottomPoint()
    {
        var screenArea = new ScreenArea(2560, 1440);
        var replayPos = new RlPoint(90, 65);
        var screenPos = screenArea.GetScreenPosition(replayPos);

        var expected = new RlPoint(1468, 1423);
        Assert.AreEqual(expected, screenPos, $"point map failed: ({expected.X},{expected.Y}) => ({screenPos.X},{screenPos.Y})");
    }
}
