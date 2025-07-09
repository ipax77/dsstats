namespace dsstats.shared.DsFen;

public static partial class DsFen
{
    public static readonly Polygon polygon2 = new Polygon(new(84, 93), new(101, 76), new(90, 65), new(73, 82));
    public static readonly Polygon polygon1 = new Polygon(new(165, 174), new(182, 157), new(171, 146), new(154, 163));

    public static string GetFen(SpawnDto spawn, Commander cmdr, int team)
    {
        var build = CmdrBuildFactory.Create(cmdr);
        if (build == null) return string.Empty;

        var polygon = team == 1 ? polygon1 : polygon2;

        var groundUnits = new Dictionary<DsPoint, char>();
        var airUnits = new Dictionary<DsPoint, char>();

        foreach (var unit in spawn.Units)
        {
            var buildOption = build.GetUnitBuildOption(unit.Unit.Name);
            if (buildOption == null) continue;

            char key = buildOption.RequiresToggle && !buildOption.IsActive
                ? char.ToUpper(buildOption.Key)
                : buildOption.Key;

            var points = GetPoints(unit.Poss);
            foreach (var p in points)
            {
                if (!polygon.IsPointInside(p))
                {
                    continue;
                }
                var grid = polygon.GetNormalizedPoint(p);
                if (buildOption.IsAir)
                    airUnits[grid] = key;
                else
                    groundUnits[grid] = key;
            }
        }

        string EncodeLayer(Dictionary<DsPoint, char> layer)
        {
            if (layer.Count == 0) return "";

            int minX = layer.Keys.Min(p => p.X);
            int maxX = layer.Keys.Max(p => p.X);
            int minY = layer.Keys.Min(p => p.Y);
            int maxY = layer.Keys.Max(p => p.Y);

            var rows = new List<string>();
            for (int y = minY; y <= maxY; y++)
            {
                string row = "";
                int empty = 0;
                for (int x = minX; x <= maxX; x++)
                {
                    if (layer.TryGetValue(new(x, y), out char key))
                    {
                        if (empty > 0)
                        {
                            row += empty;
                            empty = 0;
                        }
                        row += key;
                    }
                    else
                    {
                        empty++;
                    }
                }
                if (empty > 0) row += empty;
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
        spawn.Units.Clear();

        if (string.IsNullOrWhiteSpace(fen)) return;

        var parts = fen.Split(';');
        if (parts.Length < 2) return;

        var headerParts = parts[0].Split(':');
        if (headerParts.Length != 2) return;
        if (!int.TryParse(headerParts[0], out team)) return;
        if (!Enum.TryParse(headerParts[1], out cmdr)) return;

        var build = CmdrBuildFactory.Create(cmdr);
        if (build == null) return;

        var polygon = team == 1 ? polygon1 : polygon2;

        var layers = parts[1].Split('|');
        string groundLayer = layers.Length > 0 ? layers[0] : "";
        string airLayer = layers.Length > 1 ? layers[1] : "";

        ParseLayer(groundLayer, build, false, spawn, polygon);
        ParseLayer(airLayer, build, true, spawn, polygon);
    }

    private static void ParseLayer(string layer, CmdrBuild build, bool isAir, SpawnDto spawn, Polygon polygon)
    {
        if (string.IsNullOrWhiteSpace(layer)) return;

        var rows = layer.Split('/');
        for (int y = 0; y < rows.Length; y++)
        {
            int x = 0;
            var row = rows[y];
            for (int i = 0; i < row.Length; i++)
            {
                if (char.IsDigit(row[i]))
                {
                    string num = "";
                    while (i < row.Length && char.IsDigit(row[i]))
                        num += row[i++];
                    i--;
                    x += int.Parse(num);
                }
                else
                {
                    char ch = row[i];
                    bool isUpper = char.IsUpper(ch);
                    char key = char.ToLower(ch);

                    var name = build.GetUnitNameFromKey(key, isAir, isUpper);
                    if (name != null)
                    {
                        var gridPos = new DsPoint(x, y);
                        var world = polygon.GetDeNormalizedPoint(gridPos);

                        var unit = spawn.Units.FirstOrDefault(u => u.Unit.Name == name);
                        if (unit == null)
                        {
                            unit = new SpawnUnitDto { Unit = new UnitDto { Name = name }, Poss = "", Count = 0 };
                            spawn.Units.Add(unit);
                        }

                        if (!string.IsNullOrEmpty(unit.Poss))
                            unit.Poss += ",";
                        unit.Poss += $"{world.X},{world.Y}";
                        unit.Count++;
                    }

                    x++;
                }
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
