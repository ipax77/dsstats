using System.Text;

namespace dsstats.shared.DsFen;

public static partial class DsFen
{
    public static readonly Polygon polygon2 = new Polygon(new(84, 93), new(101, 76), new(90, 65), new(73, 82));
    public static readonly Polygon polygon1 = new Polygon(new(165, 174), new(182, 157), new(171, 146), new(154, 163));

    public static string GetFen(SpawnDto spawn, Commander cmdr, int team)
    {
        StringBuilder sb = new();
        var build = CmdrBuildFactory.Create(cmdr);
        if (build is null)
        {
            return string.Empty;
        }
        var polygon = team == 1 ? polygon1 : polygon2;

        foreach (var unit in spawn.Units)
        {
            var buildOption = build.GetUnitBuildOption(unit.Unit.Name);
            if (buildOption is null)
            {
                continue;
            }
            char unitChar = buildOption.Key;
            if (buildOption.RequiresToggle && !buildOption.IsActive)
            {
                unitChar = char.ToUpper(unitChar);
            }
            var points = GetPoints(unit.Poss);
            foreach (var point in points)
            {
                if (!polygon.IsPointInside(point))
                {
                    continue;
                    // apply fen like string
                }
                // Normalized point to a 25,17 (width,height) rectangle with bottom left at (0, 0)
                var normalizedPoint = polygon.GetNormalizedPoint(point);
            }
        }


        return sb.ToString();
    }

    public static void ApplyFen(string fen, SpawnDto spawn, out Commander cmdr, out int team)
    {
        cmdr = Commander.None;
        team = 0;
        // identify team
        // identify commander
        // getNormalizedPoints
        var polygon = team == 1 ? polygon1 : polygon2;
        var build = CmdrBuildFactory.Create(cmdr);
        if (build is null)
        {
            return;
        }

        // DeNormalize sample
        var point = new DsPoint(0, 0);
        char unitChar = 'a';
        bool isAir = false;
        bool isToggle = false;
        var unitString = build.GetUnitNameFromKey(unitChar, isAir, isToggle);
        if (string.IsNullOrEmpty(unitString))
        {
            return;
        }
        var deNormalizedPoint = polygon.GetDeNormalizedPoint(point);
        SpawnUnitDto spawnUnit = new()
        {
            Unit = new UnitDto { Name = unitString },
            Poss = $"{deNormalizedPoint.X},{deNormalizedPoint.Y}",
            Count = 1
        };
        spawn.Units.Add(spawnUnit);
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
