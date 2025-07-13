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

                var pos = mirror ? new RlPoint(u.Pos.X, -u.Pos.Y) : u.Pos;

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

    private record PlacedUnit(string UnitName, RlPoint Pos, BuildOption BuildOption);

    private record BuildRegion(UnitRegion Region, List<PlacedUnit> Units);
}

internal sealed record BuildUnit(string UnitName, RlPoint Pos, BuildOption BuildOption);