using dsstats.shared.DsFen;

namespace dsstats.builder.tests;

[TestClass]
public class PolygonNormalizerTests
{
    [TestMethod]
    public void CanNormalizePolygon()
    {
        var polygon = new Polygon(new(165, 174), new(182, 157), new(171, 146), new(154, 163));
        var points = polygon.GetAllPointsInsideOrOnEdge().ToList();
        var normalizer = new PolygonNormalizer(points, 25, 17);
        foreach (var original in points)
        {
            var normalized = normalizer.GetNormalizedPoint(original);
            Assert.IsNotNull(normalized);
            var restored = normalizer.GetDeNormalizedPoint(normalized);
            Assert.IsNotNull(restored);

            Assert.AreEqual(original.X, restored.X);
            Assert.AreEqual(original.Y, restored.Y);
        }
    }

    [TestMethod]
    public void CanNormalizeAndDeNormalizeArbitraryPoint()
    {
        var polygon = new Polygon(new(165, 174), new(182, 157), new(171, 146), new(154, 163));
        var points = polygon.GetAllPointsInsideOrOnEdge().ToList();
        var normalizer = new PolygonNormalizer(points, 25, 17);
        DsPoint testPoint = new(160, 160);
        var normalized = normalizer.GetNormalizedPoint(testPoint);
        Assert.IsNotNull(normalized, $"Normalized point should not be null for {testPoint}");
        var result = normalizer.GetDeNormalizedPoint(normalized);
        Assert.IsNotNull(result, $"De-normalized point should not be null for normalized {normalized}");
        Assert.AreEqual(testPoint.X, result.X);
        Assert.AreEqual(testPoint.Y, result.Y);
    }

    [TestMethod]
    public void CanNormalizeAndDeNormalizeArbitraryPoint2()
    {
        var polygon = new Polygon(new(165, 174), new(182, 157), new(171, 146), new(154, 163));
        var points = polygon.GetAllPointsInsideOrOnEdge().ToList();
        var normalizer = new PolygonNormalizer(points, 25, 17);
        DsPoint testPoint = new(163, 160);
        var normalized = normalizer.GetNormalizedPoint(testPoint);
        Assert.IsNotNull(normalized, $"Normalized point should not be null for {testPoint}");
        var result = normalizer.GetDeNormalizedPoint(normalized);
        Assert.IsNotNull(result, $"De-normalized point should not be null for normalized {normalized}");

        Assert.AreEqual(testPoint.X, result.X);
        Assert.AreEqual(testPoint.Y, result.Y);
    }
}
