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
    // (154, 163)
    // (165, 174)
    // (182, 157)
    // (171, 146)
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

    public List<InputEvent> GetBuildEvents(ScreenArea screenArea, CmdrBuild build, bool mirror = false)
    {
        var events = new List<InputEvent>();

        var allUnits = units
            .SelectMany(unit => unit.Value
                .OrderBy(o => o.X).ThenBy(t => t.Y)
                .Select(pos => new { unit.Key, Pos = pos }))
            .ToList();

        if (allUnits.Count == 0)
            return events;

        float yScale = screenArea._scaleY;
        int workerKey = team == 1 ? 0x31 : 0x32;

        var groupedUnits = allUnits
            .Select(u =>
            {
                var buildOption = build.GetUnitBuildOption(u.Key);
                if (buildOption is null)
                    return null;

                var pos = mirror ? MirrorPoint(u.Pos) : u.Pos;

                var screenPos = screenArea.GetScreenPosition(pos, buildOption.UnitSize);

                var region = screenPos.Y <= 15 * yScale
                    ? UnitRegion.Top
                    : screenPos.Y >= 1140 * yScale
                        ? UnitRegion.Bottom
                        : UnitRegion.Center;

                return new { Region = region, Unit = new PlacedUnit(u.Key, screenPos, buildOption) };
            })
            .Where(x => x != null)
            .GroupBy(x => x!.Region)
            .Select(g => new BuildRegion(g.Key, g.Select(x => x!.Unit).ToList()))
            .ToList();

        foreach (var region in groupedUnits.OrderBy(o => o.Region))
        {
            switch (region.Region)
            {
                case UnitRegion.Center:
                    events.AddRange(BuildRegionEvents(region.Units, build, screenArea));
                    break;

                case UnitRegion.Top:
                    events.AddRange(DsBuilder.ScrollY((int)(250 * yScale), screenArea.GetCenter()));
                    events.AddRange(BuildRegionEvents(region.Units, build, screenArea, yOffset: +125));
                    break;

                case UnitRegion.Bottom:
                    Console.WriteLine($"bottom units: {region.Units.Count}");
                    events.AddRange(DsBuilder.ScrollCenter(workerKey));
                    events.Add(new InputEvent(InputType.KeyPress, 0, 0, 0x51, 5)); // Build Menu
                    events.AddRange(DsBuilder.ScrollY((int)(-500 * yScale), screenArea.GetCenter()));
                    events.AddRange(BuildRegionEvents(region.Units, build, screenArea, yOffset: -300));
                    break;
            }
        }

        return events;
    }

    private static List<InputEvent> BuildRegionEvents(
        List<PlacedUnit> units,
        CmdrBuild build,
        ScreenArea screenArea,
        int yOffset = 0)
    {
        int offsetPixels = yOffset != 0 ? (int)(yOffset * screenArea._scaleY) : 0;

        var remaining = new List<PlacedUnit>(units);
        List<InputEvent> events = [];

        // Start from the unit closest to screen center
        RlPoint cursor = screenArea.GetCenter();
        while (remaining.Count > 0)
        {
            var next = remaining
                .OrderBy(u => cursor.DistanceSquaredTo(u.Pos))
                .First();

            remaining.Remove(next);

            var adjustedPos = next.Pos with { Y = next.Pos.Y + offsetPixels };
            events.AddRange(build.GetBuildEvents(next.UnitName, adjustedPos, screenArea, next.BuildOption));

            cursor = next.Pos;
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

    public RlPoint MirrorPoint(RlPoint p)
    {
        // Line points
        double x1 = -5.5, y1 = -5.5;
        double x2 = 11.5, y2 = -22.5;

        double dx = x2 - x1;
        double dy = y2 - y1;

        double px = p.X;
        double py = p.Y;

        // Vector from A to P
        double apx = px - x1;
        double apy = py - y1;

        // Dot product of AP and AB
        double ab_dot_ap = apx * dx + apy * dy;
        double ab_len_squared = dx * dx + dy * dy;

        // Projection scalar (2 * projection for reflection)
        double scalar = 2.0 * ab_dot_ap / ab_len_squared;

        // Projection vector scaled
        double projX = scalar * dx;
        double projY = scalar * dy;

        // Reflection = 2 * projection - original vector
        double rx = x1 + projX - apx;
        double ry = y1 + projY - apy;

        // Round to nearest int
        return new RlPoint((int)Math.Round(rx), (int)Math.Round(ry));
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
    private enum UnitRegion
    {
        Center = 0,
        Top = 1,
        Bottom = 2
    }

    public List<BuildUnit> GetBuildUnits(CmdrBuild build, int team)
    {
        List<BuildUnit> buildUnits = [];
        foreach (var ent in units)
        {
            var buildOption = build.GetUnitBuildOption(ent.Key);
            if (buildOption is null)
            {
                continue;
            }
            foreach (var pos in ent.Value)
            {
                buildUnits.Add(new(ent.Key, pos, buildOption));
            }
        }
        return buildUnits;
    }

    public List<BuildUnit> FixUnitPositions(List<BuildUnit> buildUnits)
    {
        List<RlPoint> polygon = new()
        {
            new RlPoint(0, 0),
            new RlPoint(17, -17),
            new RlPoint(6, -28),
            new RlPoint(-11, -11),
        };

        Dictionary<RlPoint, bool> airMap = GetPointsInPolygon(polygon);
        Dictionary<RlPoint, bool> groundMap = new(airMap);

        List<BuildUnit> result = [];

        foreach (var unit in buildUnits
            .OrderBy(u => u.BuildOption.UnitSize)
             .ThenBy(u => u.Pos.X)
             .ThenBy(u => u.Pos.Y))
        {
            var map = unit.BuildOption.IsAir ? airMap : groundMap;
            if (TryPlaceUnit(unit, map, out var placedPos))
            {
                var replacedUnit = unit with { Pos = placedPos };
                result.Add(replacedUnit);
            }
        }

        return result;
    }

    private bool TryPlaceUnit(
        BuildUnit unit,
        Dictionary<RlPoint, bool> occupancyMap,
        out RlPoint placedPosition)
    {
        var size = unit.BuildOption.UnitSize;
        var start = unit.Pos;
        var polygonSet = new HashSet<RlPoint>(occupancyMap.Keys);
        var visited = new HashSet<RlPoint>();
        var queue = new Queue<RlPoint>();
        queue.Enqueue(start);
        visited.Add(start);

        // Directions to explore (4-neighborhood)
        var directions = new List<RlPoint>
    {
        new(1, 0), new(-1, 0), new(0, 1), new(0, -1)
    };

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            var footprint = GetFootprint(current, size);

            // Check if all tiles in the footprint are inside the polygon and unoccupied
            if (footprint.All(p => polygonSet.Contains(p) && !occupancyMap[p]))
            {
                foreach (var p in footprint)
                {
                    occupancyMap[p] = true;
                }

                placedPosition = current;
                return true;
            }

            // Expand to neighbors
            foreach (var dir in directions)
            {
                var neighbor = new RlPoint(current.X + dir.X, current.Y + dir.Y);
                if (!visited.Contains(neighbor) && polygonSet.Contains(neighbor))
                {
                    visited.Add(neighbor);
                    queue.Enqueue(neighbor);
                }
            }
        }

        placedPosition = RlPoint.Zero;
        return false;
    }


    private List<RlPoint> GetFootprint(RlPoint center, int size)
    {
        // Assumes center is bottom-right corner of the central 2x2 square for even sizes
        List<RlPoint> footprint = [];
        int half = size / 2;

        for (int dx = -half + 1; dx <= half; dx++)
        {
            for (int dy = -half + 1; dy <= half; dy++)
            {
                footprint.Add(new RlPoint(center.X + dx, center.Y + dy));
            }
        }

        return footprint;
    }

    public static Dictionary<RlPoint, bool> GetPointsInPolygon(List<RlPoint> polygon)
    {
        var dict = new Dictionary<RlPoint, bool>();

        // Compute bounding box
        int minX = int.MaxValue, maxX = int.MinValue;
        int minY = int.MaxValue, maxY = int.MinValue;
        foreach (var pt in polygon)
        {
            minX = Math.Min(minX, pt.X);
            maxX = Math.Max(maxX, pt.X);
            minY = Math.Min(minY, pt.Y);
            maxY = Math.Max(maxY, pt.Y);
        }

        // Check each integer point within the bounding box
        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                var point = new RlPoint(x, y);
                if (IsPointInPolygon(point, polygon))
                {
                    dict[point] = false;
                }
                for (int i = 0; i < polygon.Count; i++)
                {
                    if (IsOnEdge(point, polygon[i], polygon[(i + 1) % polygon.Count]))
                        dict[point] = false;
                }
            }
        }

        return dict;
    }

    private record PlacedUnit(string UnitName, RlPoint Pos, BuildOption BuildOption);
    private record BuildRegion(UnitRegion Region, List<PlacedUnit> Units);
}

public sealed record BuildUnit(string UnitName, RlPoint Pos, BuildOption BuildOption);