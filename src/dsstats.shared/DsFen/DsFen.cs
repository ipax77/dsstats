namespace dsstats.shared.DsFen;

public static partial class DsFen
{
    public static readonly Polygon polygon2 = new Polygon(new(84, 93), new(101, 76), new(90, 65), new(73, 82));
    public static readonly Polygon polygon1 = new Polygon(new(165, 174), new(182, 157), new(171, 146), new(154, 163));
    public static PolygonNormalizer normalizer1 = new(polygon1.GetAllPointsInsideOrOnEdge().ToList(), 25, 17);
    public static PolygonNormalizer normalizer2 = new(polygon2.GetAllPointsInsideOrOnEdge().ToList(), 25, 17);

    public static string GetFen(SpawnDto spawn, Commander cmdr, int team)
    {
        var build = CmdrBuildFactory.Create(cmdr);
        if (build is null)
        {
            return string.Empty;
        }
        var polygon = team == 1 ? polygon1 : polygon2;
        var normalizer = team == 1 ? normalizer1 : normalizer2;

        DsFenGrid grid = new()
        {
            Team = team,
            Commander = cmdr,
            Units = new Dictionary<BuildOption, List<DsPoint>>()
        };
        foreach (var unit in spawn.Units)
        {
            var buildOption = build.GetUnitBuildOption(unit.Unit.Name);
            if (buildOption is null)
            {
                continue;
            }
            var points = GetPoints(unit.Poss);
            var spawnUnits = grid.Units[buildOption] = [];
            foreach (var point in points)
            {
                if (!polygon.IsPointInside(point))
                {
                    continue;
                }
                var normalizedPoint = normalizer.GetNormalizedPoint(point);
                if (normalizedPoint is null)
                {
                    continue;
                }
                spawnUnits.Add(normalizedPoint);
            }
        }
        return DsFenBuilder.GetFenString(grid);
    }

    public static void ApplyFen(string fen, SpawnDto spawn, out Commander cmdr, out int team)
    {
        var grid = DsFenBuilder.GetGridFromString(fen);
        cmdr = grid.Commander;
        team = grid.Team;
        var build = CmdrBuildFactory.Create(cmdr);
        if (build is null)
        {
            return;
        }
        var normalizer = team == 1 ? normalizer1 : normalizer2;

        foreach (var ent in grid.Units)
        {
            var unitName = build.GetUnitNameFromKey(ent.Key.Key, ent.Key.IsAir, ent.Key.RequiresToggle);
            if (string.IsNullOrEmpty(unitName))
            {
                continue;
            }
            var spawnUnit = new SpawnUnitDto()
            {
                Unit = new() { Name = unitName },
                Poss = string.Join(",", ent.Value.Select(s => normalizer.GetDeNormalizedPoint(s)).Select(t => t == null ? "" : $"{t.X},{t.Y}"))
            };
            spawn.Units.Add(spawnUnit);
        }
    }

    public static List<DsPoint> GetPoints(string possString)
    {
        if (string.IsNullOrEmpty(possString))
        {
            return [];
        }
        var stringPoints = possString.Split(',', StringSplitOptions.RemoveEmptyEntries);
        List<DsPoint> points = [];
        for (int i = 0; i < stringPoints.Length; i += 2)
        {
            points.Add(new(int.Parse(stringPoints[i]), int.Parse(stringPoints[i + 1])));
        }
        return points;
    }
}
