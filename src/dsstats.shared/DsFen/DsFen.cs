namespace dsstats.shared.DsFen;

public static partial class DsFen
{
    public static readonly Polygon polygon2 = new Polygon(new(84, 93), new(101, 76), new(90, 65), new(73, 82));
    public static readonly Polygon polygon1 = new Polygon(new(165, 174), new(182, 157), new(171, 146), new(154, 163));
    public static PolygonNormalizer normalizer1 = new(polygon1.GetAllPointsInsideOrOnEdge().ToList(), 25, 17);
    public static PolygonNormalizer normalizer2 = new(polygon2.GetAllPointsInsideOrOnEdge().ToList(), 25, 17);

    public static string GetFen(DsBuildRequest buildRequest)
    {
        var grid = GetFenGrid(buildRequest);
        if (grid is null)
        {
            return string.Empty;
        }
        return DsFenBuilder.GetFenString(grid);
    }

    private static DsFenGrid? GetFenGrid(DsBuildRequest buildRequest)
    {
        var build = CmdrBuildFactory.Create(buildRequest.Commander);
        if (build is null)
        {
            return null;
        }
        var polygon = buildRequest.Team == 1 ? polygon1 : polygon2;
        var normalizer = buildRequest.Team == 1 ? normalizer1 : normalizer2;

        DsFenGrid grid = new()
        {
            Team = buildRequest.Team,
            Commander = buildRequest.Commander,
            Units = new Dictionary<BuildOption, List<DsPoint>>()
        };
        foreach (var unit in buildRequest.Spawn.Units)
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
        foreach (var upgrade in buildRequest.Upgrades.Where(x => x.Gameloop <= buildRequest.Spawn.Gameloop))
        {
            var upgradeChar = build.GetUpgradeChar(upgrade.Upgrade.Name);
            if (upgradeChar is not null)
            {
                grid.Upgrades.Add(upgradeChar.Value);
                continue;
            }
            var abilityChar = build.GetAbilityChar(upgrade.Upgrade.Name);
            if (abilityChar is not null)
            {
                grid.Abilities.Add(abilityChar.Value);
            }
        }
        return grid;
    }

    public static string GetMirrorFen(DsBuildRequest buildRequest)
    {
        var originalGrid = GetFenGrid(buildRequest);
        if (originalGrid is null)
        {
            return string.Empty;
        }
        var mirrorGrid = DsFenBuilder.GetMirrorGrid(originalGrid);
        if (mirrorGrid is null)
        {
            return string.Empty;
        }
        return DsFenBuilder.GetFenString(mirrorGrid);
    }

    public static void ApplyFen(string fen, out DsBuildRequest buildRequest)
    {
        var grid = DsFenBuilder.GetGridFromString(fen);
        buildRequest = new DsBuildRequest();
        buildRequest.Commander = grid.Commander;
        buildRequest.Team = grid.Team;
        var build = CmdrBuildFactory.Create(buildRequest.Commander);
        if (build is null)
        {
            return;
        }
        var normalizer = buildRequest.Team == 1 ? normalizer1 : normalizer2;

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
            buildRequest.Spawn.Units.Add(spawnUnit);
        }

        foreach (var upgrade in grid.Upgrades)
        {
            var upgradeName = build.GetUpgradeName(upgrade);
            if (upgradeName is not null)
            {
                buildRequest.Upgrades.Add(new PlayerUpgradeDto() { Upgrade = new() { Name = upgradeName } });
            }
        }

        foreach (var ability in grid.Abilities)
        {
            var abilityName = build.GetAbilityName(ability);
            if (abilityName is not null)
            {
                buildRequest.Upgrades.Add(new PlayerUpgradeDto() { Upgrade = new() { Name = abilityName } });
            }
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
