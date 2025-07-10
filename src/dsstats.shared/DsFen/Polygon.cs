
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
    private double _minX;
    private double _minY;

    // new DsPoint(73, 82),   // Left
    // new DsPoint(84, 93),   // Top
    // new DsPoint(101, 76),  // Right
    // new DsPoint(90, 65),    // Bottom
    const double InvSqrt2 = 1.0 / 1.41421356237; // ≈ 1/√2

    public Polygon(DsPoint top, DsPoint right, DsPoint bottom, DsPoint left)
    {
        _top = top;
        _right = right;
        _bottom = bottom;
        _left = left;
        _vertices = new List<DsPoint> { _left, _top, _right, _bottom };
        var rotated = _vertices.Select(p =>
        {
            double x = (p.X - p.Y) * InvSqrt2;
            double y = (p.X + p.Y) * InvSqrt2;
            return new { X = x, Y = y };
        }).ToList();

        _minX = rotated.Min(p => p.X);
        _minY = rotated.Min(p => p.Y);
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


        // Step 1: Rotate all vertices by +45°
        var rotated = _vertices.Select(p =>
        {
            double x = (p.X - p.Y) * InvSqrt2;
            double y = (p.X + p.Y) * InvSqrt2;
            return new { X = x, Y = y };
        }).ToList();

        _minX = rotated.Min(p => p.X);
        _minY = rotated.Min(p => p.Y);

        var normalized = rotated
            .Select(p => new DsPoint((int)Math.Round(p.X - _minX), (int)Math.Round(p.Y - _minY)))
            .ToList();

        var bottomLeft = normalized[0];
        var topLeft = normalized[1];
        var topRight = normalized[2];
        var bottomRight = normalized[3];

        return new Polygon(topLeft, topRight, bottomRight, bottomLeft);
    }

    public DsPoint GetNormalizedPoint(DsPoint p)
    {
        double xRot = (p.X - p.Y) * InvSqrt2;
        double yRot = (p.X + p.Y) * InvSqrt2;

        int xNorm = (int)Math.Round(xRot - _minX);
        int yNorm = (int)Math.Round(yRot - _minY);

        return new DsPoint(xNorm, yNorm);
    }

    public DsPoint GetDeNormalizedPoint(DsPoint p)
    {
        // Convert back to rotated space
        double xRot = p.X + _minX;
        double yRot = p.Y + _minY;

        // Inverse rotation of +45° is -45°
        double xOrig = (xRot + yRot) * InvSqrt2;
        double yOrig = (yRot - xRot) * InvSqrt2;

        return new DsPoint((int)Math.Round(xOrig), (int)Math.Round(yOrig));
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