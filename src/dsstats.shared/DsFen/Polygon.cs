
namespace dsstats.shared.DsFen;

/// <summary>
/// representing a 45° rotated rectangle as build area
/// units can be build on every interger point (403)
/// </summary>
public sealed class Polygon
{
    private readonly DsPoint _top;
    private readonly DsPoint _right;
    private readonly DsPoint _bottom;
    private readonly DsPoint _left;
    private readonly List<DsPoint> _vertices;
    private readonly HashSet<DsPoint> _allPoints;

    // new DsPoint(73, 82),   // Left
    // new DsPoint(84, 93),   // Top
    // new DsPoint(101, 76),  // Right
    // new DsPoint(90, 65),    // Bottom
    public Polygon(DsPoint top, DsPoint right, DsPoint bottom, DsPoint left)
    {
        _top = top;
        _right = right;
        _bottom = bottom;
        _left = left;
        _vertices = new List<DsPoint> { _left, _top, _right, _bottom };
        _allPoints = GetAllPointsInsideOrOnEdge().ToHashSet();
    }

    public List<DsPoint> GetVertices()
    {
        return _vertices;
    }

    /// <summary>
    /// Rotate Polygon by -45° and normalize _left (then bottom left) to 0, 0
    /// </summary>
    /// <returns></returns>
    public Polygon GetNormalizedPolygon()
    {
        const double InvSqrt2 = 1.0 / 1.41421356237; // ≈ 1/√2

        // Step 1: Rotate all vertices by -45°
        var rotated = _vertices.Select(p =>
        {
            double x = (p.X + p.Y) * InvSqrt2;
            double y = (p.Y - p.X) * InvSqrt2;
            return new { X = x, Y = y };
        }).ToList();

        double minX = rotated.Min(p => p.X);
        double minY = rotated.Min(p => p.Y);

        var normalized = rotated
            .Select(p => new DsPoint((int)Math.Round(p.X - minX), (int)Math.Round(p.Y - minY)))
            .ToList();

        return new Polygon(normalized[1], normalized[2], normalized[3], normalized[0]);
    }

    public DsPoint GetNormalizedPoint(DsPoint p)
    {
        const double InvSqrt2 = 1.0 / 1.41421356237;

        // Rotate input point
        double rotatedX = (p.X + p.Y) * InvSqrt2;
        double rotatedY = (p.Y - p.X) * InvSqrt2;

        // Rotate the _left reference point (this becomes (0, 0) after normalization)
        double baseX = (_left.X + _left.Y) * InvSqrt2;
        double baseY = (_left.Y - _left.X) * InvSqrt2;

        // Normalize relative to _left
        double normX = rotatedX - baseX;
        double normY = rotatedY - baseY;

        return new DsPoint((int)Math.Round(normX), (int)Math.Round(normY));
    }

    public DsPoint GetDeNormalizedPoint(DsPoint p)
    {
        const double InvSqrt2 = 1.0 / 1.41421356237;

        // Step 1: Rotate _left by -45° to get the normalization origin
        double baseX = (_left.X + _left.Y) * InvSqrt2;
        double baseY = (_left.Y - _left.X) * InvSqrt2;

        // Step 2: Denormalize (add the offset back)
        double rotatedX = p.X + baseX;
        double rotatedY = p.Y + baseY;

        // Step 3: Rotate by +45° to get the original coordinates
        double originalX = (rotatedX - rotatedY) * InvSqrt2;
        double originalY = (rotatedX + rotatedY) * InvSqrt2;

        return new DsPoint((int)Math.Round(originalX), (int)Math.Round(originalY));
    }

    public bool IsPointInside(DsPoint p)
    {
        return _allPoints.Contains(p);
    }

    public List<DsPoint> GetAllPointsInsideOrOnEdge()
    {
        var points = new List<DsPoint>();
        var minX = _vertices.Min(v => v.X);
        var maxX = _vertices.Max(v => v.X);
        var minY = _vertices.Min(v => v.Y);
        var maxY = _vertices.Max(v => v.Y);

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                var p = new DsPoint(x, y);
                bool isInside = true;
                for (int i = 0; i < _vertices.Count; i++)
                {
                    var p1 = _vertices[i];
                    var p2 = _vertices[(i + 1) % _vertices.Count];
                    var crossProduct = (p.X - p1.X) * (p2.Y - p1.Y) - (p.Y - p1.Y) * (p2.X - p1.X);
                    if (crossProduct < 0)
                    {
                        isInside = false;
                        break;
                    }
                }

                if (isInside)
                {
                    points.Add(p);
                }
            }
        }

        return points;
    }
}

public sealed record DsPoint(int X, int Y);