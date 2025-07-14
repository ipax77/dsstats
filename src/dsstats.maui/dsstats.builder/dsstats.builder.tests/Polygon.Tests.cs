using dsstats.shared.DsFen;

namespace dsstats.builder.tests;

[TestClass]
public sealed class PolygonTests
{
    [TestMethod]
    public void CanNormalizePolygon()
    {
        var polygon = new Polygon(new(165, 174), new(182, 157), new(171, 146), new(154, 163)); // top, right, bottom, left
        var normalizedPolygon = polygon.GetNormalizedPolygon();
        var vertices = normalizedPolygon.GetVertices(); //  _left, _top, _right, _bottom
        var normalizedBottomLeft = vertices[0];
        var normalizedTopLeft = vertices[1];
        var normalizedTopRight = vertices[2];
        var normalizedBottomRight = vertices[3];
        Assert.AreEqual(0, normalizedBottomLeft.X, $"({normalizedBottomLeft.X},{normalizedBottomLeft.Y})");
        Assert.AreEqual(0, normalizedBottomLeft.Y, $"({normalizedBottomLeft.X},{normalizedBottomLeft.Y})");

        Assert.AreEqual(0, normalizedTopLeft.X, $"({normalizedTopLeft.X},{normalizedTopLeft.Y})");
        Assert.AreEqual(16, normalizedTopLeft.Y, $"({normalizedTopLeft.X},{normalizedTopLeft.Y})");

        Assert.AreEqual(24, normalizedTopRight.X, $"({normalizedTopRight.X},{normalizedTopRight.Y})");
        Assert.AreEqual(16, normalizedTopRight.Y, $"({normalizedTopRight.X},{normalizedTopRight.Y})");

        Assert.AreEqual(24, normalizedBottomRight.X, $"({normalizedBottomRight.X},{normalizedBottomRight.Y})");
        Assert.AreEqual(0, normalizedBottomRight.Y, $"({normalizedBottomRight.X},{normalizedBottomRight.Y})");
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
    public void CanNormalizePoint2()
    {
        var polygon = new Polygon(new(165, 174), new(182, 157), new(171, 146), new(154, 163));
        DsPoint point = new(160, 160);
        var normalizedPoint = polygon.GetNormalizedPoint(point);
        Assert.IsTrue(normalizedPoint.X >= 0, $"({normalizedPoint.X},{normalizedPoint.Y})");
        Assert.IsTrue(normalizedPoint.Y >= 0, $"({normalizedPoint.X},{normalizedPoint.Y})");
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

    [TestMethod]
    public void Team2CanNormalizePoint()
    {
        var polygon = new Polygon(new(84, 93), new(101, 76), new(90, 65), new(73, 82));
        DsPoint point = new(73, 82);
        var normalizedPoint = polygon.GetNormalizedPoint(point);
        Assert.AreEqual(0, normalizedPoint.X);
        Assert.AreEqual(0, normalizedPoint.Y);
    }

    [TestMethod]
    public void Team2CanDeNormalizePoint()
    {
        var polygon = new Polygon(new(84, 93), new(101, 76), new(90, 65), new(73, 82));
        DsPoint normalizedPoint = new(0, 0);
        var point = polygon.GetDeNormalizedPoint(normalizedPoint);
        Assert.AreEqual(73, point.X, $"({normalizedPoint.X},{normalizedPoint.Y}) => ({point.X},{point.Y})");
        Assert.AreEqual(82, point.Y, $"({normalizedPoint.X},{normalizedPoint.Y}) => ({point.X},{point.Y})");
    }

    [TestMethod]
    public void Team2CanGetAllPoint()
    {
        var polygon = new Polygon(new(84, 93), new(101, 76), new(90, 65), new(73, 82));
        var points = polygon.GetAllPointsInsideOrOnEdge();
        Assert.AreEqual(403, points.Count());
    }

    [TestMethod]
    public void CanNormalizeAllPoint()
    {
        var polygon = new Polygon(new(84, 93), new(101, 76), new(90, 65), new(73, 82));
        var points = polygon.GetAllPointsInsideOrOnEdge();
        HashSet<DsDPoint> normalizedPoints = [];
        foreach (var point in points.OrderBy(o => o.X).ThenBy(o => o.Y))
        {
            var normalizedPoint = polygon.GetNormalizedDoublePoint(point);
            Assert.IsFalse(normalizedPoints.Contains(normalizedPoint), $"Duplicate normalized point: ({normalizedPoint.X},{normalizedPoint.Y})");
            normalizedPoints.Add(normalizedPoint);
            Console.WriteLine($"({point.X},{point.Y}) => ({normalizedPoint.X},{normalizedPoint.Y})");
            Assert.IsTrue(normalizedPoint.X >= 0, $"({normalizedPoint.X},{normalizedPoint.Y})");
            Assert.IsTrue(normalizedPoint.Y >= 0, $"({normalizedPoint.X},{normalizedPoint.Y})");
        }
    }
}

