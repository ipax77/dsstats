
namespace dsstats.shared.DsFen;

public class PolygonNormalizer
{
    private readonly Dictionary<DsPoint, DsPoint> _toNormalized = new();
    private readonly Dictionary<DsPoint, DsPoint> _toOriginal = new();

    public PolygonNormalizer(List<DsPoint> allPointsInside, int normalizedWidth, int normalizedHeight)
    {
        // Sort consistently — here by (Y, then X) = scanline order
        var sortedPoints = allPointsInside.OrderBy(p => p.Y).ThenBy(p => p.X).ToList();

        int i = 0;
        foreach (var point in sortedPoints)
        {
            int x = i % normalizedWidth;
            int y = i / normalizedWidth;

            var normalized = new DsPoint(x, y);
            _toNormalized[point] = normalized;
            _toOriginal[normalized] = point;
            i++;
        }
    }

    public DsPoint? GetNormalizedPoint(DsPoint p) =>
        _toNormalized.TryGetValue(p, out var n) ? n :
        null;

    public DsPoint? GetDeNormalizedPoint(DsPoint p) =>
        _toOriginal.TryGetValue(p, out var o) ? o :
        null;
}
