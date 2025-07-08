namespace dsstats.shared.DsFen;

public static class DsFen
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

        if (groundUnits.Count == 0 && airUnits.Count == 0)
            return string.Empty;

        var allPoints = groundUnits.Keys.Concat(airUnits.Keys).ToList();
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

        return $"{team}:{cmdr};{groundFen}|{airFen}";
    }

    public static void ApplyFen(string fen, SpawnDto spawn, out Commander cmdr, out int team)
    {
        cmdr = Commander.None;
        team = 0;

        if (string.IsNullOrWhiteSpace(fen))
            return;

        // Split metadata and layers
        var parts = fen.Split(';');
        if (parts.Length != 2)
            return;

        var header = parts[0]; // e.g., "2:Zerg"
        var layers = parts[1].Split('|');

        var headerParts = header.Split(':');
        if (headerParts.Length != 2)
            return;

        if (!int.TryParse(headerParts[0], out team))
            return;

        if (!Enum.TryParse(headerParts[1], out cmdr))
            return;

        var build = CmdrBuildFactory.Create(cmdr);
        if (build == null)
            return;

        spawn.Units.Clear();
        var polygon = team == 1 ? polygon1 : polygon2;

        string groundLayer = layers.Length > 0 ? layers[0] : "";
        string airLayer = layers.Length > 1 ? layers[1] : "";

        ParseLayer(groundLayer, build, isAir: false, spawn, polygon);
        ParseLayer(airLayer, build, isAir: true, spawn, polygon);
    }

    private static void ParseLayer(string layer, CmdrBuild build, bool isAir, SpawnDto spawn, List<(int x, int y)> polygon)
    {
        if (string.IsNullOrWhiteSpace(layer))
            return;

        int y = 0;
        var rows = layer.Split('/');
        foreach (var row in rows)
        {
            int x = 0;
            foreach (char ch in row)
            {
                if (char.IsDigit(ch))
                {
                    x += ch - '0';
                }
                else
                {
                    bool isUpper = char.IsUpper(ch);
                    char key = char.ToLower(ch);

                    string? unitName = build.GetUnitNameFromKey(key, isAir, isUpper);
                    if (unitName != null)
                    {
                        var normPos = (x, y);
                        var absPos = DenormalizeFromTop(normPos, polygon);

                        var spawnUnit = spawn.Units.FirstOrDefault(u => u.Unit.Name == unitName);
                        if (spawnUnit == null)
                        {
                            spawnUnit = new SpawnUnitDto { Unit = new UnitDto { Name = unitName }, Poss = "" };
                            spawn.Units.Add(spawnUnit);
                        }

                        if (!string.IsNullOrEmpty(spawnUnit.Poss))
                            spawnUnit.Poss += ",";
                        spawnUnit.Poss += $"{absPos.x},{absPos.y}";
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

    private static (int x, int y) NormalizeToTop((int x, int y) point, List<(int x, int y)> polygon)
    {
        var top = polygon[1]; // Top corner is reference
        return new(point.x - top.x, point.y - top.y);
    }
}
