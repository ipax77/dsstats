namespace dsstats.builder.tests;

[TestClass]
public sealed class PolygonTests
{
    [TestMethod]
    public void CanNormalizePolygon()
    {
        var polygon = new Polygon(new(165, 174), new(182, 157), new(171, 146), new(154, 163));
        var normalizedPolygon = polygon.GetNormalizedPolygon();
        var vertices = normalizedPolygon.GetVertices();
        var bottomLeft = vertices[3];
        Assert.AreEqual(0, bottomLeft.X);
        Assert.AreEqual(0, bottomLeft.Y);
    }

    [TestMethod]
    public void CanNormalizePoint()
    {
        var polygon = new Polygon(new(165, 174), new(182, 157), new(171, 146), new(154, 163));
        DsPoint point = new(154, 163);
        var normalizedPoint = polygon.GetNormalizedPoint(point);
        Assert.AreEqual(0, normalizedPoint.X);
        Assert.AreEqual(0, normalizedPoint.Y);
    }

    [TestMethod]
    public void CanDeNormalizePoint()
    {
        var polygon = new Polygon(new(165, 174), new(182, 157), new(171, 146), new(154, 163));
        DsPoint normalizedPoint = new(0, 0);
        var point = polygon.GetDeNormalizedPoint(normalizedPoint);
        Assert.AreEqual(154, point.X, $"({normalizedPoint.X},{normalizedPoint.Y}) => ({point.X},{point.Y})");
        Assert.AreEqual(163, point.Y, $"({normalizedPoint.X},{normalizedPoint.Y}) => ({point.X},{point.Y})");
    }

    [TestMethod]
    public void NormalizedRoundTripReturnsOriginal()
    {
        var polygon = new Polygon(new(165, 174), new(182, 157), new(171, 146), new(154, 163));
        DsPoint original = new(160, 170);
        var normalized = polygon.GetNormalizedPoint(original);
        var denormalized = polygon.GetDeNormalizedPoint(normalized);

        Assert.AreEqual(original.X, denormalized.X);
        Assert.AreEqual(original.Y, denormalized.Y);
    }

    [TestMethod]
    public void CanNormalizeAndDeNormalizeArbitraryPoint()
    {
        var polygon = new Polygon(new(165, 174), new(182, 157), new(171, 146), new(154, 163));
        DsPoint testPoint = new(160, 160);
        var normalized = polygon.GetNormalizedPoint(testPoint);
        var result = polygon.GetDeNormalizedPoint(normalized);

        Assert.AreEqual(testPoint.X, result.X);
        Assert.AreEqual(testPoint.Y, result.Y);
    }

    [TestMethod]
    public void CanGetAllPoint()
    {
        var polygon = new Polygon(new(165, 174), new(182, 157), new(171, 146), new(154, 163));
        var points = polygon.GetAllPointsInsideOrOnEdge();
        Assert.AreEqual(403, points.Count());
    }
}

