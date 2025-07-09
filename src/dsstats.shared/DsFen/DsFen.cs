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
    private const double CenterX = 128.0;
    private const double CenterY = 120.0;

    public static string GetFen(SpawnDto spawn, Commander cmdr, int team)
    {
        var build = CmdrBuildFactory.Create(cmdr);
        if (build == null) return string.Empty;

        var polygon = team == 1 ? polygon1 : polygon2;
        var origin = polygon[3]; // Bottom point is origin

        var groundUnits = new Dictionary<(int x, int y), char>();
        var airUnits = new Dictionary<(int x, int y), char>();

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
                if (!IsPointInsideOrOnEdge(p, team))
                {
                    continue;
                }
                var grid = RotateNeg45Int(p);
                if (buildOption.IsAir)
                    airUnits[grid] = key;
                else
                    groundUnits[grid] = key;
            }
        }

        string EncodeLayer(Dictionary<(int x, int y), char> layer)
        {
            if (layer.Count == 0) return "";

            int minX = layer.Keys.Min(p => p.x);
            int maxX = layer.Keys.Max(p => p.x);
            int minY = layer.Keys.Min(p => p.y);
            int maxY = layer.Keys.Max(p => p.y);

            var rows = new List<string>();
            for (int y = minY; y <= maxY; y++)
            {
                string row = "";
                int empty = 0;
                for (int x = minX; x <= maxX; x++)
                {
                    if (layer.TryGetValue((x, y), out char key))
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
        var origin = polygon[3];

        var layers = parts[1].Split('|');
        string groundLayer = layers.Length > 0 ? layers[0] : "";
        string airLayer = layers.Length > 1 ? layers[1] : "";

        ParseLayer(groundLayer, build, false, spawn, origin);
        ParseLayer(airLayer, build, true, spawn, origin);
    }

    private static void ParseLayer(string layer, CmdrBuild build, bool isAir, SpawnDto spawn, (int x, int y) origin)
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
                        var gridPos = (x, y);
                        var world = RotatePos45Int(gridPos);

                        var unit = spawn.Units.FirstOrDefault(u => u.Unit.Name == name);
                        if (unit == null)
                        {
                            unit = new SpawnUnitDto { Unit = new UnitDto { Name = name }, Poss = "", Count = 0 };
                            spawn.Units.Add(unit);
                        }

                        if (!string.IsNullOrEmpty(unit.Poss))
                            unit.Poss += ",";
                        unit.Poss += $"{world.x},{world.y}";
                        unit.Count++;
                    }

                    x++;
                }
            }
        }
    }

    private static (double x, double y) Rotate((int x, int y) pt, double angleDeg)
    {
        double angleRad = angleDeg * Math.PI / 180.0;
        double cos = Math.Cos(angleRad);
        double sin = Math.Sin(angleRad);

        double dx = pt.x - CenterX;
        double dy = pt.y - CenterY;

        double x = cos * dx - sin * dy + CenterX;
        double y = sin * dx + cos * dy + CenterY;

        return (x, y);
    }

    private static (int x, int y) RotateNeg45Int((int x, int y) pt)
    {
        var rotated = Rotate(pt, -45);
        return ((int)Math.Round(rotated.x), (int)Math.Round(rotated.y));
    }

    private static (int x, int y) RotatePos45Int((int x, int y) pt)
    {
        var rotated = Rotate(pt, +45);
        return ((int)Math.Round(rotated.x), (int)Math.Round(rotated.y));
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

    private static bool IsPointInsideOrOnEdge((int x, int y) p, int team)
    {
        var polygon = team == 1 ? polygon1 : polygon2;
        if (IsPointInPolygon(p, polygon))
            return true;

        for (int i = 0; i < polygon.Count; i++)
        {
            if (IsOnEdge(p, polygon[i], polygon[(i + 1) % polygon.Count]))
                return true;
        }

        return false;
    }

    private static bool IsPointInPolygon((int x, int y) p, List<(int x, int y)> polygon)
    {
        int wn = 0; // winding number
        int n = polygon.Count;

        for (int i = 0; i < n; i++)
        {
            (int x, int y) pi = polygon[i];
            (int x, int y) pj = polygon[(i + 1) % n];

            if (pi.y <= p.y)
            {
                if (pj.y > p.y && IsLeft(pi, pj, p) > 0)
                    wn++;
            }
            else
            {
                if (pj.y <= p.y && IsLeft(pi, pj, p) < 0)
                    wn--;
            }
        }

        return wn != 0;
    }

    private static double IsLeft((int x, int y) p0, (int x, int y) p1, (int x, int y) p2)
    {
        return (p1.x - p0.x) * (p2.y - p0.y) - (p2.x - p0.x) * (p1.y - p0.y);
    }

    private static bool IsOnEdge((int x, int y) p, (int x, int y) a, (int x, int y) b)
    {
        double cross = (p.y - a.y) * (b.x - a.x) - (p.x - a.x) * (b.y - a.y);
        if (Math.Abs(cross) > 1e-6) return false;

        double dot = (p.x - a.x) * (b.x - a.x) + (p.y - a.y) * (b.y - a.y);
        if (dot < 0) return false;

        double lenSq = (b.x - a.x) * (b.x - a.x) + (b.y - a.y) * (b.y - a.y);
        return dot <= lenSq;
    }
}
