using dsstats.shared.DsFen;

namespace dsstats.builder;

/// <summary>
/// describes a 45° rotated rectangle of squares as used in the Starcraft II map editor of the Direct Strike map
/// sc2 replays contain the unit coordinates based on it
/// each interger coordinate inside the polygan can hold one ground and one air unit
/// the unrotated rectangle does have 12 rows with 18 columns and alternately 11 rows with 17 columns as a grid of diamonds
/// </summary>
public class BuildArea
{
    private List<RlPoint> polygon = [];
    private List<RlPoint> polygon2 =
    [
        new RlPoint(73, 82),   // Left
        new RlPoint(84, 93),   // Top
        new RlPoint(101, 76),  // Right
        new RlPoint(90, 65),    // Bottom
    ];
    private List<RlPoint> polygon1 =
    [
        new RlPoint(154, 163),   // Left
        new RlPoint(165, 174),    // Top
        new RlPoint(182, 157),  // Right
        new RlPoint(171, 146),   // Bottom
    ];
    private Dictionary<string, HashSet<RlPoint>> units = [];
    private int team = 0;

    public BuildArea(int team)
    {
        this.team = team;
        polygon = team == 1 ? polygon1 : polygon2;
    }

    public List<InputEvent> GetBuildEvents(ScreenArea screenArea, CmdrBuild build)
    {
        List<InputEvent> events = [];
        var allUnits = units.SelectMany(unit =>
            unit.Value
                .OrderBy(o => o.X).ThenBy(t => t.Y)
                .Select(pos => new BuildUnit(unit.Key, pos)))
            .ToList();
        int worker = team == 1 ? 0x31 : 0x32;

        if (allUnits.Count == 0)
        {
            return events;
        }

        List<BuildUnit> topUnits = [];
        List<BuildUnit> centerUnits = [];
        List<BuildUnit> bottomUnits = [];
        WorkerMenu workerMenu = new();

        foreach (var unit in allUnits)
        {

            var screenPos = screenArea.GetScreenPosition(unit.Pos);
            if (screenPos.Y <= 15)
            {
                topUnits.Add(new(unit.UnitName, screenPos));
            }
            else if (screenPos.Y >= 1140)
            {
                bottomUnits.Add(new(unit.UnitName, screenPos));
            }
            else
            {
                centerUnits.Add(new(unit.UnitName, screenPos));
            }
        }

        foreach (var centerUnit in centerUnits)
        {
            events.AddRange(build.GetBuildEvents(centerUnit.UnitName, centerUnit.Pos, screenArea, workerMenu));
        }
        if (topUnits.Count > 0)
        {
            events.AddRange(DsBuilder.ScrollY(Convert.ToInt32(250 * screenArea._scaleY), screenArea.GetCenter()));
            foreach (var topUnit in topUnits)
            {
                events.AddRange(build.GetBuildEvents(topUnit.UnitName,
                    topUnit.Pos with { Y = topUnit.Pos.Y + Convert.ToInt32(125 * screenArea._scaleY) }, screenArea, workerMenu));
            }
        }
        if (bottomUnits.Count > 0)
        {
            Console.WriteLine($"bottom units: {bottomUnits.Count}");
            events.AddRange(DsBuilder.ScrollCenter(worker));
            events.Add(new(InputType.KeyPress, 0, 0, 0x51, 5)); // Build Menu
            events.AddRange(DsBuilder.ScrollY(Convert.ToInt32(-500 * screenArea._scaleY), screenArea.GetCenter()));
            foreach (var bottomUnit in bottomUnits)
            {
                events.AddRange(build.GetBuildEvents(bottomUnit.UnitName,
                    bottomUnit.Pos with { Y = bottomUnit.Pos.Y - Convert.ToInt32(300 * screenArea._scaleY) }, screenArea, workerMenu));
            }
        }

        return events;
    }

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
        unitPositions.Add(NormalizeToTop(position));
        return true;
    }

    public void PlaceUnits(string unit, string positions, int team)
    {
        if (string.IsNullOrEmpty(positions))
        {
            return;
        }
        if (!units.TryGetValue(unit, out var unitPositions) || unitPositions == null)
        {
            unitPositions = units[unit] = [];
        }
        var newUnitPositions = GetUnitPositions(positions, team);
        foreach (var pos in newUnitPositions)
        {
            unitPositions.Add(pos);
        }
    }

    private List<RlPoint> GetUnitPositions(string unitString, int team)
    {
        var stringPoints = unitString.Split(',', StringSplitOptions.RemoveEmptyEntries);
        List<RlPoint> mapPoints = [];
        for (int i = 0; i < stringPoints.Length; i += 2)
        {
            RlPoint mapPoint = new(int.Parse(stringPoints[i]), int.Parse(stringPoints[i + 1]));
            if (IsPointInsideOrOnEdge(mapPoint))
            {
                mapPoints.Add(NormalizeToTop(mapPoint));
            }
        }
        return mapPoints;
    }

    public RlPoint NormalizeToTop(RlPoint point)
    {
        var top = polygon[1]; // Top corner is reference
        return new RlPoint(point.X - top.X, point.Y - top.Y);
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

internal sealed record BuildUnit(string UnitName, RlPoint Pos);