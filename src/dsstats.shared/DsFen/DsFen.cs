namespace dsstats.shared.DsFen;

public static partial class DsFen
{
    private static readonly List<(int x, int y)> polygon2 =
    [
        new (73, 82),   // Left
        new (84, 93),   // Top
        new (101, 76),  // Right
        new (90, 65),    // Bottom
    ];
    private static readonly List<(int x, int y)> polygon1 =
    [
        new (154, 163),   // Left
        new (165, 174),    // Top
        new (182, 157),  // Right
        new (171, 146),   // Bottom
    ];

    public static string GetFen(SpawnDto spawn, Commander cmdr, int team)
    {
        var build = CmdrBuildFactory.Create(cmdr);
        if (build == null)
        {
            return string.Empty;
        }

        var polygon = team == 1 ? polygon1 : polygon2;

        var groundUnits = new Dictionary<(int x, int y), char>();
        var airUnits = new Dictionary<(int x, int y), char>();

        foreach (var unit in spawn.Units)
        {
            var buildOption = build.GetUnitBuildOption(unit.Unit.Name);
            if (buildOption == null)
                continue;

            char key = buildOption.RequiresToggle && !buildOption.IsActive
                ? char.ToUpper(buildOption.Key)
                : buildOption.Key;

            var points = GetPoints(unit.Poss).Select(s => NormalizeToTop(s, polygon));
            foreach (var point in points)
            {
                if (buildOption.IsAir)
                    airUnits[point] = key;
                else
                    groundUnits[point] = key;
            }
        }

        var allPoints = groundUnits.Keys.Concat(airUnits.Keys).ToList();
        if (!allPoints.Any())
        {
            return $"{team}:{cmdr};0,0;|";
        }

        int minX = allPoints.Min(p => p.x);
        int maxX = allPoints.Max(p => p.x);
        int minY = allPoints.Min(p => p.y);
        int maxY = allPoints.Max(p => p.y);

        string EncodeLayer(Dictionary<(int x, int y), char> layer)
        {
            var rows = new List<string>();
            for (int y = minY; y <= maxY; y++)
            {
                string row = "";
                int emptyCount = 0;

                for (int x = minX; x <= maxX; x++)
                {
                    var pt = (x, y);
                    if (layer.TryGetValue(pt, out char key))
                    {
                        if (emptyCount > 0)
                        {
                            row += emptyCount.ToString();
                            emptyCount = 0;
                        }
                        row += key;
                    }
                    else
                    {
                        emptyCount++;
                    }
                }

                if (emptyCount > 0)
                    row += emptyCount.ToString();

                rows.Add(row);
            }

            return string.Join("/", rows);
        }

        var groundFen = EncodeLayer(groundUnits);
        var airFen = EncodeLayer(airUnits);

        return $"{team}:{cmdr};{minX},{minY};{groundFen}|{airFen}";
    }

    public static void ApplyFen(string fen, SpawnDto spawn, out Commander cmdr, out int team)
    {
        cmdr = Commander.None;
        team = 0;

        if (string.IsNullOrWhiteSpace(fen))
            return;

        var parts = fen.Split(';');
        if (parts.Length < 2) return;

        var header = parts[0];
        var headerParts = header.Split(':');
        if (headerParts.Length != 2) return;
        if (!int.TryParse(headerParts[0], out team)) return;
        if (!Enum.TryParse(headerParts[1], out cmdr)) return;

        var build = CmdrBuildFactory.Create(cmdr);
        if (build == null) return;
        
        spawn.Units.Clear();
        var polygon = team == 1 ? polygon1 : polygon2;

        int minX = 0, minY = 0;
        string[] layers;

        if (parts.Length == 3) // New format: team:cmdr;minX,minY;ground|air
        {
            var originParts = parts[1].Split(',');
            if (originParts.Length != 2) return;
            if (!int.TryParse(originParts[0], out minX)) return;
            if (!int.TryParse(originParts[1], out minY)) return;
            layers = parts[2].Split('|');
        }
        else // Old format: team:cmdr;ground|air
        {
            layers = parts[1].Split('|');
        }

        string groundLayer = layers.Length > 0 ? layers[0] : "";
        string airLayer = layers.Length > 1 ? layers[1] : "";

        ParseLayer(groundLayer, build, isAir: false, spawn, polygon, minX, minY);
        ParseLayer(airLayer, build, isAir: true, spawn, polygon, minX, minY);
    }

    private static void ParseLayer(string layer, CmdrBuild build, bool isAir, SpawnDto spawn, List<(int x, int y)> polygon, int minX, int minY)
    {
        if (string.IsNullOrWhiteSpace(layer))
            return;

        int y = 0;
        var rows = layer.Split('/');
        foreach (var row in rows)
        {
            int x = 0;
            for (int i = 0; i < row.Length; i++)
            {
                if (char.IsDigit(row[i]))
                {
                    string numStr = "";
                    while (i < row.Length && char.IsDigit(row[i]))
                    {
                        numStr += row[i];
                        i++;
                    }
                    i--; 
                    x += int.Parse(numStr);
                }
                else
                {
                    char ch = row[i];
                    bool isUpper = char.IsUpper(ch);
                    char key = char.ToLower(ch);

                    string? unitName = build.GetUnitNameFromKey(key, isAir, isUpper);
                    if (unitName != null)
                    {
                        var normPos = (x + minX, y + minY);
                        var absPos = DenormalizeFromTop(normPos, polygon);

                        var spawnUnit = spawn.Units.FirstOrDefault(u => u.Unit.Name == unitName);
                        if (spawnUnit == null)
                        {
                            spawnUnit = new SpawnUnitDto { Count = 0, Unit = new UnitDto { Name = unitName }, Poss = "" };
                            spawn.Units.Add(spawnUnit);
                        }

                        if (!string.IsNullOrEmpty(spawnUnit.Poss))
                            spawnUnit.Poss += ",";
                        spawnUnit.Poss += $"{absPos.x},{absPos.y}";
                        spawnUnit.Count++;
                    }
                    x++;
                }
            }
            y++;
        }
    }

    private static (int x, int y) DenormalizeFromTop((int x, int y) normPos, List<(int x, int y)> polygon)
    {
        var top = polygon[1]; // Top corner is reference
        return (normPos.x + top.x, normPos.y + top.y);
    }


    private static List<(int x, int y)> GetPoints(string possString)
    {
        if (string.IsNullOrEmpty(possString))
        {
            return [];
        }
        var stringPoints = possString.Split(',', StringSplitOptions.RemoveEmptyEntries);
        List<(int x, int y)> points = [];
        for (int i = 0; i < stringPoints.Length; i += 2)
        {
            points.Add((int.Parse(stringPoints[i]), int.Parse(stringPoints[i + 1])));
        }
        return points;
    }

    internal static (int x, int y) NormalizeToTop((int x, int y) point, List<(int x, int y)> polygon)
    {
        var top = polygon[1]; // Top corner is reference
        return new(point.x - top.x, point.y - top.y);
    }
}
