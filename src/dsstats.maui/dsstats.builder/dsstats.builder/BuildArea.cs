namespace dsstats.builder;

/// <summary>
/// describes a 45° rotated rectangle of squares as used in the Starcraft II map editor of the Direct Strike map
/// sc2 replays contain the unit coordinates based on it
/// each interger coordinate inside the polygan can hold one ground and one air unit
/// the unrotated rectangle does have 12 rows with 18 columns and alternately 11 rows with 17 columns as a grid of diamonds
/// </summary>
public class BuildArea
{
    private List<RlPoint> polygon =
    [
        new RlPoint(73, 82),   // Left
        new RlPoint(84, 93),   // Top
        new RlPoint(101, 76),  // Right
        new RlPoint(90, 65),    // Bottom
    ];
    private Dictionary<string, HashSet<RlPoint>> units = [];

    public bool PlaceUnit(string unit, RlPoint position)
    {
        if (!IsPointInsideOrOnEdge(position))
        {
            return false;
        }
        if (!units.TryGetValue(unit, out var unitPositions) || unitPositions == null)
        {
            unitPositions = units[unit] = [];
        }
        unitPositions.Add(position);
        return true;
    }

    public RlPoint GetCenter()
    {
        int x1 = polygon.Min(m => m.X);
        int y1 = polygon.Min(m => m.Y);
        int x2 = polygon.Max(m => m.X);
        int y2 = polygon.Max(m => m.Y);

        return new(x1 + ((x2 - x1) / 2), y1 + ((y2 - y1) / 2));
    }

    private bool IsPointInsideOrOnEdge(RlPoint p)
    {
        if (IsPointInPolygon(p, polygon))
            return true;

        for (int i = 0; i < polygon.Count; i++)
        {
            if (IsOnEdge(p, polygon[i], polygon[(i + 1) % polygon.Count]))
                return true;
        }

        return false;
    }

    private static bool IsPointInPolygon(RlPoint p, List<RlPoint> polygon)
    {
        int wn = 0; // winding number
        int n = polygon.Count;

        for (int i = 0; i < n; i++)
        {
            RlPoint pi = polygon[i];
            RlPoint pj = polygon[(i + 1) % n];

            if (pi.Y <= p.Y)
            {
                if (pj.Y > p.Y && IsLeft(pi, pj, p) > 0)
                    wn++;
            }
            else
            {
                if (pj.Y <= p.Y && IsLeft(pi, pj, p) < 0)
                    wn--;
            }
        }

        return wn != 0;
    }

    private static double IsLeft(RlPoint p0, RlPoint p1, RlPoint p2)
    {
        return (p1.X - p0.X) * (p2.Y - p0.Y) - (p2.X - p0.X) * (p1.Y - p0.Y);
    }

    private static bool IsOnEdge(RlPoint p, RlPoint a, RlPoint b)
    {
        double cross = (p.Y - a.Y) * (b.X - a.X) - (p.X - a.X) * (b.Y - a.Y);
        if (Math.Abs(cross) > 1e-6) return false;

        double dot = (p.X - a.X) * (b.X - a.X) + (p.Y - a.Y) * (b.Y - a.Y);
        if (dot < 0) return false;

        double lenSq = (b.X - a.X) * (b.X - a.X) + (b.Y - a.Y) * (b.Y - a.Y);
        return dot <= lenSq;
    }
}

