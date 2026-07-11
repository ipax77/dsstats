using System.Globalization;
using System.Text;

namespace dsstats.shared.Builder;

public sealed record SpawnBuilderFenResult(
    Commander Commander,
    int Team,
    SpawnDto Spawn,
    IReadOnlyList<UpgradeDto> Upgrades);

public static class SpawnBuilderFen
{
    public const ushort FormatVersion = 1;
    public const int Width = 29;
    public const int Height = 29;

    public readonly record struct Cell(int X, int Y);
    private const string Prefix = "DSF1";

    private static readonly BuildGrid Team1Grid = BuildGrid.Create(
        new(165, 174), new(182, 157), new(171, 146), new(154, 163));
    private static readonly BuildGrid Team2Grid = BuildGrid.Create(
        new(84, 93), new(101, 76), new(90, 65), new(73, 82));

    public static string Encode(
        Commander commander,
        int team,
        SpawnDto spawn,
        IReadOnlyList<UpgradeDto>? upgrades = null)
    {
        ArgumentNullException.ThrowIfNull(spawn);
        ValidateMetadata(commander, team);

        var grid = GetGrid(team);
        char[] ground = new char[Width * Height];
        char[] air = new char[Width * Height];

        foreach (var unit in spawn.Units)
        {
            if (!BuilderUnitCatalog.TryGetUnit(commander, unit.Name, out var definition)
                || unit.Positions is not { Count: >= 2 } positions)
            {
                continue;
            }

            var board = definition.IsAir ? air : ground;
            for (var index = 0; index + 1 < positions.Count; index += 2)
            {
                if (grid.TryNormalize(new(positions[index], positions[index + 1]), out var normalized))
                {
                    board[normalized.Y * Width + normalized.X] = definition.Symbol;
                }
            }
        }

        StringBuilder output = new(160);
        output.Append(Prefix).Append(' ')
            .Append((int)commander).Append(' ')
            .Append(team).Append(' ');
        EncodeBoard(ground, output);
        output.Append('|');
        EncodeBoard(air, output);
        output.Append('|');
        if (upgrades is not null)
        {
            foreach (var upgrade in upgrades)
            {
                if (BuilderUnitCatalog.TryGetUpgrade(commander, upgrade.Name, out var definition))
                {
                    output.Append(definition.Symbol);
                }
            }
        }
        return output.ToString();
    }

    public static bool TryDecode(string? fen, out SpawnBuilderFenResult result)
    {
        if (string.IsNullOrWhiteSpace(fen))
        {
            result = null!;
            return false;
        }

        try
        {
            result = Decode(fen);
            return true;
        }
        catch (FormatException)
        {
            result = null!;
            return false;
        }
        catch (ArgumentOutOfRangeException)
        {
            result = null!;
            return false;
        }
    }

    public static SpawnBuilderFenResult Decode(string fen)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fen);
        fen = fen.Trim();
        var parts = fen.AsSpan();
        var prefixEnd = parts.IndexOf(' ');
        if (prefixEnd < 0 || !parts[..prefixEnd].SequenceEqual(Prefix))
        {
            throw new FormatException("Unsupported builder FEN version.");
        }

        parts = parts[(prefixEnd + 1)..];
        var commanderValue = ReadInt(ref parts);
        var team = ReadInt(ref parts);
        var commander = (Commander)commanderValue;
        ValidateMetadata(commander, team);

        var layerSeparator = parts.IndexOf('|');
        if (layerSeparator < 0)
        {
            throw new FormatException("Builder FEN must contain ground and air layers.");
        }

        var upgradeSeparator = parts[(layerSeparator + 1)..].IndexOf('|');
        var airLayer = upgradeSeparator < 0
            ? parts[(layerSeparator + 1)..]
            : parts.Slice(layerSeparator + 1, upgradeSeparator);
        var upgradeLayer = upgradeSeparator < 0
            ? ReadOnlySpan<char>.Empty
            : parts[(layerSeparator + upgradeSeparator + 2)..];

        var grid = GetGrid(team);
        Dictionary<char, List<int>> positionsBySymbol = [];
        DecodeBoard(parts[..layerSeparator], commander, false, grid, positionsBySymbol);
        DecodeBoard(airLayer, commander, true, grid, positionsBySymbol);

        List<UnitDto> units = new(positionsBySymbol.Count);
        foreach (var definition in BuilderUnitCatalog.GetUnits(commander))
        {
            if (positionsBySymbol.TryGetValue(definition.Symbol, out var positions))
            {
                units.Add(new UnitDto
                {
                    Name = definition.Name,
                    Count = positions.Count / 2,
                    Positions = positions
                });
            }
        }

        List<UpgradeDto> upgrades = new(upgradeLayer.Length);
        foreach (var symbol in upgradeLayer)
        {
            if (!BuilderUnitCatalog.TryGetUpgrade(commander, symbol, out var definition))
            {
                throw new FormatException("Invalid upgrade symbol in builder FEN.");
            }
            upgrades.Add(new() { Name = definition.Name });
        }

        return new(commander, team, new SpawnDto { Units = units }, upgrades);
    }

    public static string Mirror(string fen)
    {
        var decoded = Decode(fen);
        var targetTeam = decoded.Team == 1 ? 2 : 1;
        var sourceTop = decoded.Team == 1 ? new GridPoint(165, 174) : new GridPoint(84, 93);
        var targetTop = targetTeam == 1 ? new GridPoint(165, 174) : new GridPoint(84, 93);
        foreach (var unit in decoded.Spawn.Units)
        {
            var positions = unit.Positions;
            if (positions is null)
            {
                continue;
            }

            for (var index = 0; index + 1 < positions.Count; index += 2)
            {
                var x = positions[index] - sourceTop.X;
                var y = positions[index + 1] - sourceTop.Y;
                // Reflect around the build area's top-left to bottom-right center line.
                const double lineX = -5.5;
                const double lineY = -5.5;
                const double directionX = 17;
                const double directionY = -17;
                var projection = 2 * ((x - lineX) * directionX + (y - lineY) * directionY)
                    / (directionX * directionX + directionY * directionY);
                positions[index] = targetTop.X + (int)Math.Round(lineX + projection * directionX - (x - lineX));
                positions[index + 1] = targetTop.Y + (int)Math.Round(lineY + projection * directionY - (y - lineY));
            }
        }

        return Encode(decoded.Commander, targetTeam, decoded.Spawn, decoded.Upgrades);
    }

    public static BuilderRequest Mirror(BuilderRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var mirrored = Decode(Mirror(Encode(
            request.Commander,
            request.Team,
            request.Spawn,
            request.Upgrades)));
        return request with
        {
            Team = mirrored.Team,
            Spawn = mirrored.Spawn,
            Upgrades = mirrored.Upgrades,
            Mirror = false
        };
    }

    private static void EncodeBoard(ReadOnlySpan<char> board, StringBuilder output)
    {
        for (var y = Height - 1; y >= 0; y--)
        {
            var empty = 0;
            for (var x = 0; x < Width; x++)
            {
                var symbol = board[y * Width + x];
                if (symbol == '\0')
                {
                    empty++;
                    continue;
                }

                if (empty > 0)
                {
                    output.Append(empty);
                    empty = 0;
                }
                output.Append(symbol);
            }

            if (empty > 0)
            {
                output.Append(empty);
            }
            if (y > 0)
            {
                output.Append('/');
            }
        }
    }

    private static void DecodeBoard(
        ReadOnlySpan<char> encoded,
        Commander commander,
        bool isAir,
        BuildGrid grid,
        Dictionary<char, List<int>> positionsBySymbol)
    {
        var x = 0;
        var encodedRow = 0;
        for (var index = 0; index <= encoded.Length; index++)
        {
            var value = index < encoded.Length ? encoded[index] : '/';
            if (value == '/')
            {
                if (x != Width || encodedRow >= Height)
                {
                    throw new FormatException("Invalid builder FEN row width.");
                }
                x = 0;
                encodedRow++;
                continue;
            }

            if (char.IsAsciiDigit(value))
            {
                var count = value - '0';
                while (index + 1 < encoded.Length && char.IsAsciiDigit(encoded[index + 1]))
                {
                    count = checked(count * 10 + encoded[++index] - '0');
                }
                x += count;
                continue;
            }

            if (x >= Width || !BuilderUnitCatalog.TryGetUnit(commander, value, out var definition)
                || definition.IsAir != isAir)
            {
                throw new FormatException("Invalid unit symbol in builder FEN.");
            }

            var normalized = new GridPoint(x++, Height - 1 - encodedRow);
            if (!grid.TryDenormalize(normalized, out var mapPoint))
            {
                throw new FormatException("Unit is outside the Direct Strike build area.");
            }
            if (!positionsBySymbol.TryGetValue(value, out var positions))
            {
                positions = [];
                positionsBySymbol[value] = positions;
            }
            positions.Add(mapPoint.X);
            positions.Add(mapPoint.Y);
        }

        if (encodedRow != Height)
        {
            throw new FormatException("Invalid builder FEN row count.");
        }
    }

    private static int ReadInt(ref ReadOnlySpan<char> remaining)
    {
        var separator = remaining.IndexOf(' ');
        if (separator <= 0 || !int.TryParse(remaining[..separator], NumberStyles.None, CultureInfo.InvariantCulture, out var value))
        {
            throw new FormatException("Invalid builder FEN metadata.");
        }
        remaining = remaining[(separator + 1)..];
        return value;
    }

    private static void ValidateMetadata(Commander commander, int team)
    {
        if (!BuilderUnitCatalog.IsSupported(commander))
        {
            throw new ArgumentOutOfRangeException(nameof(commander), commander, "Commander is not supported by the builder.");
        }
        if (team is not (1 or 2))
        {
            throw new ArgumentOutOfRangeException(nameof(team), team, "Team must be 1 or 2.");
        }
    }

    private static BuildGrid GetGrid(int team) => team == 1 ? Team1Grid : Team2Grid;

    public static bool TryGetCell(int team, int mapX, int mapY, out Cell cell)
    {
        if (team is not (1 or 2)
            || !GetGrid(team).TryNormalize(new(mapX, mapY), out var normalized))
        {
            cell = default;
            return false;
        }
        cell = new(normalized.X, normalized.Y);
        return true;
    }

    public static bool TryGetMapPosition(int team, Cell cell, out int mapX, out int mapY)
    {
        if (team is not (1 or 2)
            || !GetGrid(team).TryDenormalize(new(cell.X, cell.Y), out var position))
        {
            mapX = mapY = 0;
            return false;
        }
        mapX = position.X;
        mapY = position.Y;
        return true;
    }

    public static bool IsValidCell(int team, Cell cell) =>
        team is 1 or 2 && GetGrid(team).TryDenormalize(new(cell.X, cell.Y), out _);

    private readonly record struct GridPoint(int X, int Y);

    private sealed class BuildGrid
    {
        private readonly GridPoint origin;
        private readonly GridPoint[] vertices;

        private BuildGrid(GridPoint origin, GridPoint[] vertices)
        {
            this.origin = origin;
            this.vertices = vertices;
        }

        public static BuildGrid Create(GridPoint top, GridPoint right, GridPoint bottom, GridPoint left)
        {
            GridPoint[] vertices = [left, top, right, bottom];
            var minX = vertices.Min(point => point.X);
            var maxX = vertices.Max(point => point.X);
            var minY = vertices.Min(point => point.Y);
            var maxY = vertices.Max(point => point.Y);
            if (maxX - minX + 1 != Width || maxY - minY + 1 != Height)
            {
                throw new InvalidOperationException("Builder grid dimensions do not match its map bounds.");
            }
            return new(new(minX, minY), vertices);
        }

        public bool TryNormalize(GridPoint point, out GridPoint normalized)
        {
            if (IsInside(point, vertices))
            {
                normalized = new(point.X - origin.X, point.Y - origin.Y);
                return true;
            }
            normalized = default;
            return false;
        }

        public bool TryDenormalize(GridPoint point, out GridPoint mapPoint)
        {
            if ((uint)point.X < Width && (uint)point.Y < Height)
            {
                var candidate = new GridPoint(origin.X + point.X, origin.Y + point.Y);
                if (IsInside(candidate, vertices))
                {
                    mapPoint = candidate;
                    return true;
                }
            }
            mapPoint = default;
            return false;
        }

        private static bool IsInside(GridPoint point, ReadOnlySpan<GridPoint> vertices)
        {
            for (var index = 0; index < vertices.Length; index++)
            {
                var first = vertices[index];
                var second = vertices[(index + 1) % vertices.Length];
                var cross = (point.X - first.X) * (second.Y - first.Y)
                    - (point.Y - first.Y) * (second.X - first.X);
                if (cross < 0)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
