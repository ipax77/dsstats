using dsstats.shared;
using dsstats.shared.DsFen;
using System.Text;

public static class DsFenBuilder
{
    private const int Width = 25;
    private const int Height = 17;

    public static string GetFenString(DsFenGrid grid)
    {
        var groundBoard = new char[Height, Width];
        var airBoard = new char[Height, Width];

        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
            {
                groundBoard[y, x] = ' ';
                airBoard[y, x] = ' ';
            }

        foreach (var kvp in grid.Units)
        {
            char symbol = kvp.Key.Key;
            if (kvp.Key.RequiresToggle && !kvp.Key.IsActive)
                symbol = char.ToUpper(symbol);

            foreach (var point in kvp.Value)
            {
                if (point.X < 0 || point.X >= Width || point.Y < 0 || point.Y >= Height)
                    continue;

                if (kvp.Key.IsAir)
                    airBoard[point.Y, point.X] = symbol;
                else
                    groundBoard[point.Y, point.X] = symbol;
            }
        }

        string EncodeBoard(char[,] board)
        {
            var sb = new StringBuilder();

            for (int y = Height - 1; y >= 0; y--)
            {
                int emptyCount = 0;
                for (int x = 0; x < Width; x++)
                {
                    char c = board[y, x];
                    if (c == ' ')
                    {
                        emptyCount++;
                    }
                    else
                    {
                        if (emptyCount > 0)
                        {
                            sb.Append(emptyCount);
                            emptyCount = 0;
                        }
                        sb.Append(c);
                    }
                }

                if (emptyCount > 0)
                    sb.Append(emptyCount);

                if (y != 0)
                    sb.Append('/');
            }

            return sb.ToString();
        }

        var groundFen = EncodeBoard(groundBoard);
        var airFen = EncodeBoard(airBoard);
        var upgrades = new string(grid.Upgrades?.ToArray() ?? []);
        var abilities = new string(grid.Abilities?.ToArray() ?? []);

        return $"{groundFen}|{airFen} {grid.Team} {(int)grid.Commander} {upgrades} {abilities}";
    }


    public static DsFenGrid GetGridFromString(string fen)
    {
        const int Width = 25;
        const int Height = 17;

        var parts = fen.Split(' ');
        if (parts.Length < 3)
            throw new ArgumentException("Invalid FEN string: missing metadata");

        var layers = parts[0].Split('|');
        if (layers.Length != 2)
            throw new ArgumentException("Invalid FEN: missing air/ground layers");

        string groundLayer = layers[0];
        string airLayer = layers[1];

        var units = new Dictionary<BuildOption, List<DsPoint>>();

        void DecodeLayer(string layer, bool isAir)
        {
            var rows = layer.Split('/');
            if (rows.Length != Height)
                throw new ArgumentException("Invalid layer: incorrect number of rows");

            for (int y = 0; y < Height; y++)
            {
                string row = rows[y];
                int boardY = Height - 1 - y;
                int x = 0;

                for (int i = 0; i < row.Length;)
                {
                    if (char.IsDigit(row[i]))
                    {
                        int start = i;
                        while (i < row.Length && char.IsDigit(row[i]))
                            i++;

                        string numberStr = row.Substring(start, i - start);
                        x += int.Parse(numberStr);
                    }
                    else
                    {
                        char symbol = row[i];
                        bool requiresToggle = char.IsUpper(symbol);

                        var buildOption = new BuildOption(symbol, IsAir: isAir, RequiresToggle: requiresToggle);

                        if (!units.ContainsKey(buildOption))
                            units[buildOption] = new List<DsPoint>();

                        units[buildOption].Add(new DsPoint(x, boardY));
                        x++;
                        i++;
                    }
                }

                if (x != Width)
                    throw new ArgumentException($"Invalid FEN row at Y={boardY}: row length mismatch");
            }
        }

        DecodeLayer(groundLayer, isAir: false);
        DecodeLayer(airLayer, isAir: true);

        if (!int.TryParse(parts[1], out int team))
            throw new ArgumentException("Invalid team value");

        if (!int.TryParse(parts[2], out int commanderVal))
            throw new ArgumentException("Invalid commander");

        Commander commander = Enum.IsDefined(typeof(Commander), commanderVal)
            ? (Commander)commanderVal
            : Commander.None;

        var upgrades = parts.Length > 3 ? parts[3].ToCharArray().ToList() : [];
        var abilities = parts.Length > 4 ? parts[4].ToCharArray().ToList() : [];

        return new DsFenGrid
        {
            Units = units,
            Team = team,
            Commander = commander,
            Upgrades = upgrades,
            Abilities = abilities
        };
    }

}



public record DsFenGrid
{
    public int Team { get; init; }
    public Commander Commander { get; init; }
    public Dictionary<BuildOption, List<DsPoint>> Units { get; init; } = new();
    public List<char> Upgrades { get; init; } = [];
    public List<char> Abilities { get; init; } = [];

}