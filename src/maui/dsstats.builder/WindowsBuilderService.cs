using System.ComponentModel;
using System.Runtime.InteropServices;
using dsstats.shared;
using dsstats.shared.Builder;

namespace dsstats.builder;

public sealed class WindowsBuilderService : IBuilderService
{
    public bool IsAvailable => OperatingSystem.IsWindows();
    public bool Supports(Commander commander) => IsAvailable && BuilderUnitCatalog.IsSupported(commander);

    public Task BuildAsync(BuilderRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!Supports(request.Commander))
        {
            throw new NotSupportedException($"{request.Commander} is not supported by the Direct Strike builder.");
        }

        return Task.Run(async () =>
        {
            var effectiveRequest = request;
            if (request.Mirror)
            {
                var mirrored = SpawnBuilderFen.Decode(SpawnBuilderFen.Mirror(
                    SpawnBuilderFen.Encode(request.Commander, request.Team, request.Spawn)));
                effectiveRequest = request with
                {
                    Team = mirrored.Team,
                    Spawn = mirrored.Spawn,
                    Upgrades = mirrored.Upgrades,
                    Mirror = false
                };
            }

            var width = NativeMethods.GetSystemMetrics(NativeMethods.SmCxScreen);
            var height = NativeMethods.GetSystemMetrics(NativeMethods.SmCyScreen);
            if (width <= 0 || height <= 0)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not determine the primary display size.");
            }

            var actions = BuildPlanner.CreateActions(effectiveRequest, width, height);
            await InputPlayer.PlayAsync(actions, cancellationToken).ConfigureAwait(false);
        }, cancellationToken);
    }
}

internal static class BuildPlanner
{
    private const int ShortDelay = 15;

    public static List<BuilderAction> CreateActions(BuilderRequest request, int screenWidth, int screenHeight)
    {
        var screen = new ScreenTransform(request.Team, screenWidth, screenHeight);
        List<BuilderAction> actions = new(Math.Max(64, request.Spawn.Units.Sum(unit => unit.Count) * 3));
        AddSetup(actions, request.Commander, request.Team, screen);

        var placedUnits = PlacementPlanner.Place(request);
        placedUnits.Sort((left, right) =>
        {
            var leftScreen = screen.Map(left.Position, left.Definition.Footprint);
            var rightScreen = screen.Map(right.Position, right.Definition.Footprint);
            var region = GetRegion(leftScreen, screen).CompareTo(GetRegion(rightScreen, screen));
            if (region != 0) return region;
            var y = leftScreen.Y.CompareTo(rightScreen.Y);
            return y != 0 ? y : leftScreen.X.CompareTo(rightScreen.X);
        });

        actions.Add(BuilderAction.Key('Q', ShortDelay));
        Dictionary<char, bool> toggleStates = [];
        var currentRegion = -1;
        foreach (var unit in placedUnits)
        {
            var screenPoint = screen.Map(unit.Position, unit.Definition.Footprint);
            var region = GetRegion(screenPoint, screen);
            if (region != currentRegion)
            {
                currentRegion = region;
                if (region == 1)
                {
                    AddScroll(actions, screen, 250);
                }
                else if (region == 2)
                {
                    var worker = request.Team == 1 ? '1' : '2';
                    actions.Add(BuilderAction.Key(worker, ShortDelay));
                    actions.Add(BuilderAction.Key(worker, ShortDelay));
                    actions.Add(BuilderAction.Key('Q', ShortDelay));
                    AddScroll(actions, screen, -500);
                }
            }

            var definition = unit.Definition;
            if (definition.RequiresToggle)
            {
                var active = toggleStates.TryGetValue(definition.BuildKey, out var state) ? state : true;
                if (active != definition.IsDefaultToggleState)
                {
                    AddToggle(actions, definition.BuildKey, screen);
                    toggleStates[definition.BuildKey] = definition.IsDefaultToggleState;
                }
            }

            screenPoint = region switch
            {
                1 => screen.OffsetY(screenPoint, 125),
                2 => screen.OffsetY(screenPoint, -300),
                _ => screenPoint
            };
            if (definition.IsAbility)
            {
                var worker = request.Team == 1 ? '1' : '2';
                actions.Add(BuilderAction.Key(worker, ShortDelay));
                actions.Add(BuilderAction.Key('W', ShortDelay));
                AddBuildUnit(actions, definition.BuildKey, screenPoint);
                actions.Add(BuilderAction.Key(worker, ShortDelay));
                actions.Add(BuilderAction.Key('Q', ShortDelay));
            }
            else
            {
                AddBuildUnit(actions, definition.BuildKey, screenPoint);
            }
        }

        AddUpgrades(actions, request);

        return actions;
    }

    private static void AddUpgrades(List<BuilderAction> actions, BuilderRequest request)
    {
        if (request.Upgrades is not { Count: > 0 } upgrades)
        {
            return;
        }

        List<BuilderUpgradeDefinition> abilities = [];
        List<BuilderUpgradeDefinition> armoryUpgrades = [];
        foreach (var upgrade in upgrades.OrderBy(upgrade => upgrade.Gameloop))
        {
            if (!BuilderUnitCatalog.TryGetUpgrade(request.Commander, upgrade.Name, out var definition))
            {
                continue;
            }
            (definition.IsAbility ? abilities : armoryUpgrades).Add(definition);
        }

        var worker = request.Team == 1 ? '1' : '2';
        if (abilities.Count > 0)
        {
            actions.Add(BuilderAction.Key(worker, 100));
            actions.Add(BuilderAction.Key('W', 100));
            foreach (var ability in abilities)
            {
                actions.Add(BuilderAction.Key(ability.BuildKey, 200));
            }
        }

        if (armoryUpgrades.Count > 0)
        {
            actions.Add(BuilderAction.Key(worker, 100));
            foreach (var upgrade in armoryUpgrades)
            {
                actions.Add(BuilderAction.Key(upgrade.BuildKey, 200));
            }
        }
    }

    private static int GetRegion(PixelPoint point, ScreenTransform screen) =>
        point.Y <= 15 * screen.ScaleY ? 1 : point.Y >= 1140 * screen.ScaleY ? 2 : 0;

    private static void AddScroll(List<BuilderAction> actions, ScreenTransform screen, int logicalOffset)
    {
        actions.Add(BuilderAction.Move(screen.Center, ShortDelay, middle: true));
        var remaining = Math.Abs((int)(logicalOffset * screen.ScaleY));
        var direction = Math.Sign(logicalOffset);
        while (remaining > 0)
        {
            var step = Math.Min(8, remaining);
            actions.Add(BuilderAction.RelativeMove(0, step * direction, 1, middle: true));
            remaining -= step;
        }
        actions.Add(BuilderAction.Move(screen.Center, ShortDelay));
    }

    private static void AddSetup(List<BuilderAction> actions, Commander commander, int team, ScreenTransform screen)
    {
        for (var index = 0; index < 5; index++)
        {
            actions.Add(BuilderAction.VirtualKey(NativeMethods.VkPrior, ShortDelay));
        }

        var worker = team == 1 ? '1' : '2';
        actions.Add(BuilderAction.Key(worker, ShortDelay));
        actions.Add(BuilderAction.Key(worker, ShortDelay));
        AddChatCommand(actions, "Infinite");
        AddChatCommand(actions, "Tier");

        if (team == 1)
        {
            AddChatCommand(actions, "Repick");
            var column = commander switch
            {
                Commander.Protoss => 0,
                Commander.Terran => 1,
                Commander.Zerg => 2,
                _ => throw new NotSupportedException()
            };
            var repick = screen.Scale(new(2107 + column * 89, 992));
            actions.Add(BuilderAction.Click(repick, 250));
        }
        else
        {
            AddChatCommand(actions, $"Enemy {commander}");
        }

        var center = screen.Center;
        actions.Add(BuilderAction.Move(center, 150));
        actions.Add(BuilderAction.Click(center, 100));
        actions.Add(BuilderAction.Key(worker, 200, control: true));
    }

    private static void AddChatCommand(List<BuilderAction> actions, string command)
    {
        actions.Add(BuilderAction.VirtualKey(NativeMethods.VkReturn, ShortDelay));
        foreach (var character in command)
        {
            actions.Add(BuilderAction.Key(character, ShortDelay));
        }
        actions.Add(BuilderAction.VirtualKey(NativeMethods.VkReturn, ShortDelay));
    }

    private static void AddBuildUnit(List<BuilderAction> actions, char key, PixelPoint position)
    {
        actions.Add(BuilderAction.Move(position, ShortDelay));
        actions.Add(BuilderAction.Key(key, ShortDelay));
        actions.Add(BuilderAction.Click(position, ShortDelay));
    }

    private static void AddToggle(List<BuilderAction> actions, char key, ScreenTransform screen)
    {
        const string keys = "qwertasdfgzxcvb";
        var index = keys.IndexOf(char.ToLowerInvariant(key));
        if (index < 0)
        {
            return;
        }
        var row = index / 5;
        var column = index % 5;
        var position = screen.Scale(new(2106 + column * 89, 1179 + row * 88));
        actions.Add(BuilderAction.Move(position, 100));
        actions.Add(BuilderAction.RightClick(position, 30));
        actions.Add(BuilderAction.Move(screen.Center, 100));
    }
}

internal static class PlacementPlanner
{
    private const int MinX = -11;
    private const int MaxX = 17;
    private const int MinY = -28;
    private const int MaxY = 0;
    private const int Side = 29;
    private static readonly GridPoint[] Directions =
        [new(1, 1), new(1, -1), new(-1, 1), new(-1, -1), new(1, 0), new(-1, 0), new(0, 1), new(0, -1)];
    private static readonly GridPoint[] SmallFootprint = [new(0, 0)];
    private static readonly GridPoint[] MediumFootprint = [new(0, 0), new(-1, 0), new(-1, 1), new(0, 1)];
    private static readonly GridPoint[] LargeFootprint =
        [new(0, 0), new(-1, 0), new(-1, 1), new(0, 1), new(1, 1), new(1, 0), new(1, -1), new(0, -1), new(-1, -1)];

    public static List<PlacedUnit> Place(BuilderRequest request)
    {
        var top = request.Team == 1 ? new GridPoint(165, 174) : new GridPoint(84, 93);
        List<PlacedUnit> source = new(request.Spawn.Units.Sum(unit => unit.Count));
        foreach (var unit in request.Spawn.Units)
        {
            if (!BuilderUnitCatalog.TryGetUnit(request.Commander, unit.Name, out var definition)
                || unit.Positions is not { Count: >= 2 } positions)
            {
                continue;
            }
            for (var index = 0; index + 1 < positions.Count; index += 2)
            {
                var point = new GridPoint(positions[index] - top.X, positions[index + 1] - top.Y);
                if (IsInside(point))
                {
                    source.Add(new(definition, point));
                }
            }
        }

        source.Sort(static (left, right) =>
        {
            var footprint = left.Definition.Footprint.CompareTo(right.Definition.Footprint);
            if (footprint != 0) return footprint;
            var x = left.Position.X.CompareTo(right.Position.X);
            return x != 0 ? x : left.Position.Y.CompareTo(right.Position.Y);
        });

        var ground = new bool[Side * Side];
        var air = new bool[Side * Side];
        var visited = new int[Side * Side];
        var queue = new GridPoint[Side * Side];
        var generation = 0;
        List<PlacedUnit> result = new(source.Count);
        foreach (var unit in source)
        {
            var occupancy = unit.Definition.IsAir ? air : ground;
            if (TryPlace(unit.Position, unit.Definition.Footprint, occupancy, visited, queue, ++generation, out var position))
            {
                result.Add(unit with { Position = position });
            }
        }
        return result;
    }

    private static bool TryPlace(
        GridPoint start,
        int footprint,
        bool[] occupancy,
        int[] visited,
        GridPoint[] queue,
        int generation,
        out GridPoint result)
    {
        var head = 0;
        var tail = 0;
        queue[tail++] = start;
        visited[ToIndex(start)] = generation;
        while (head < tail)
        {
            var current = queue[head++];
            if (CanOccupy(current, footprint, occupancy))
            {
                Occupy(current, footprint, occupancy);
                result = current;
                return true;
            }

            foreach (var direction in Directions)
            {
                var next = new GridPoint(current.X + direction.X, current.Y + direction.Y);
                if (!IsInside(next)) continue;
                var index = ToIndex(next);
                if (visited[index] == generation) continue;
                visited[index] = generation;
                queue[tail++] = next;
            }
        }

        result = default;
        return false;
    }

    private static bool CanOccupy(GridPoint center, int size, bool[] occupancy)
    {
        foreach (var offset in GetFootprint(size))
        {
            var point = new GridPoint(center.X + offset.X, center.Y + offset.Y);
            if (!IsInside(point) || occupancy[ToIndex(point)]) return false;
        }
        return true;
    }

    private static void Occupy(GridPoint center, int size, bool[] occupancy)
    {
        foreach (var offset in GetFootprint(size))
        {
            occupancy[ToIndex(new(center.X + offset.X, center.Y + offset.Y))] = true;
        }
    }

    private static ReadOnlySpan<GridPoint> GetFootprint(int size) => size switch
    {
        1 => SmallFootprint,
        2 => MediumFootprint,
        _ => LargeFootprint
    };

    private static bool IsInside(GridPoint point)
    {
        if (point.X is < MinX or > MaxX || point.Y is < MinY or > MaxY) return false;
        // Diamond vertices: top, right, bottom, left.
        ReadOnlySpan<GridPoint> vertices = [new(-11, -11), new(0, 0), new(17, -17), new(6, -28)];
        for (var index = 0; index < vertices.Length; index++)
        {
            var first = vertices[index];
            var second = vertices[(index + 1) % vertices.Length];
            var cross = (point.X - first.X) * (second.Y - first.Y)
                - (point.Y - first.Y) * (second.X - first.X);
            if (cross < 0) return false;
        }
        return true;
    }

    private static int ToIndex(GridPoint point) => (point.Y - MinY) * Side + point.X - MinX;
}

internal sealed class ScreenTransform
{
    private readonly double scaleX;
    private readonly double scaleY;
    private readonly Homography homography;

    public ScreenTransform(int team, int width, int height)
    {
        scaleX = width / 2560d;
        scaleY = height / 1440d;
        Center = Scale(team == 1 ? new PixelPoint(1410, 470) : new PixelPoint(1278, 581));
        PixelPoint[] destination = team == 1
            ? [new(1124, -110), new(2100, 765), new(1468, 1423), new(485, 437)]
            : [new(1128, -50), new(2114, 828), new(1469, 1503), new(482, 498)];
        homography = new(
            [new(0, 0), new(17, -17), new(6, -28), new(-11, -11)],
            destination);
    }

    public PixelPoint Center { get; }
    public double ScaleY => scaleY;
    public PixelPoint Scale(PixelPoint point) => new((int)(point.X * scaleX), (int)(point.Y * scaleY));
    public PixelPoint OffsetY(PixelPoint point, int logicalOffset) => new(point.X, point.Y + (int)(logicalOffset * scaleY));

    public PixelPoint Map(GridPoint point, int footprint)
    {
        var x = footprint % 2 == 0 ? point.X - .5 : point.X;
        var y = footprint % 2 == 0 ? point.Y + .5 : point.Y;
        return Scale(homography.Transform(x, y));
    }
}

internal sealed class Homography
{
    private readonly double[] matrix;

    public Homography(ReadOnlySpan<GridPoint> source, ReadOnlySpan<PixelPoint> destination)
    {
        Span<double> augmented = stackalloc double[8 * 9];
        for (var index = 0; index < 4; index++)
        {
            var row = index * 2;
            var x = source[index].X;
            var y = source[index].Y;
            var u = destination[index].X;
            var v = destination[index].Y;
            SetRow(augmented, row, x, y, u, true);
            SetRow(augmented, row + 1, x, y, v, false);
        }

        Solve(augmented);
        matrix = new double[9];
        for (var index = 0; index < 8; index++) matrix[index] = augmented[index * 9 + 8];
        matrix[8] = 1;
    }

    public PixelPoint Transform(double x, double y)
    {
        var denominator = matrix[6] * x + matrix[7] * y + 1;
        return new(
            (int)((matrix[0] * x + matrix[1] * y + matrix[2]) / denominator),
            (int)((matrix[3] * x + matrix[4] * y + matrix[5]) / denominator));
    }

    private static void SetRow(Span<double> data, int row, double x, double y, double target, bool horizontal)
    {
        var offset = row * 9;
        if (horizontal)
        {
            data[offset] = x; data[offset + 1] = y; data[offset + 2] = 1;
            data[offset + 6] = -x * target; data[offset + 7] = -y * target;
        }
        else
        {
            data[offset + 3] = x; data[offset + 4] = y; data[offset + 5] = 1;
            data[offset + 6] = -x * target; data[offset + 7] = -y * target;
        }
        data[offset + 8] = target;
    }

    private static void Solve(Span<double> data)
    {
        for (var pivot = 0; pivot < 8; pivot++)
        {
            var best = pivot;
            for (var row = pivot + 1; row < 8; row++)
            {
                if (Math.Abs(data[row * 9 + pivot]) > Math.Abs(data[best * 9 + pivot])) best = row;
            }
            if (best != pivot)
            {
                for (var column = pivot; column < 9; column++)
                    (data[pivot * 9 + column], data[best * 9 + column]) = (data[best * 9 + column], data[pivot * 9 + column]);
            }
            var divisor = data[pivot * 9 + pivot];
            if (Math.Abs(divisor) < 1e-10) throw new InvalidOperationException("Invalid screen calibration.");
            for (var column = pivot; column < 9; column++) data[pivot * 9 + column] /= divisor;
            for (var row = 0; row < 8; row++)
            {
                if (row == pivot) continue;
                var factor = data[row * 9 + pivot];
                for (var column = pivot; column < 9; column++) data[row * 9 + column] -= factor * data[pivot * 9 + column];
            }
        }
    }
}

internal static class InputPlayer
{
    public static async Task PlayAsync(IReadOnlyList<BuilderAction> actions, CancellationToken cancellationToken)
    {
        var shiftDown = false;
        var controlDown = false;
        var middleDown = false;
        try
        {
            foreach (var action in actions)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (action.DelayMs > 0) await Task.Delay(action.DelayMs, cancellationToken).ConfigureAwait(false);
                SetModifier(NativeMethods.VkShift, action.Shift, ref shiftDown);
                SetModifier(NativeMethods.VkControl, action.Control, ref controlDown);
                if (action.Middle != middleDown)
                {
                    NativeMethods.SetMiddleButton(action.Middle);
                    middleDown = action.Middle;
                }
                switch (action.Kind)
                {
                    case BuilderActionKind.Key:
                        NativeMethods.PressKey((byte)action.KeyCode);
                        break;
                    case BuilderActionKind.Move:
                        NativeMethods.SetCursorPos(action.X, action.Y);
                        break;
                    case BuilderActionKind.Click:
                        NativeMethods.SetCursorPos(action.X, action.Y);
                        NativeMethods.Click(false);
                        break;
                    case BuilderActionKind.RightClick:
                        NativeMethods.SetCursorPos(action.X, action.Y);
                        NativeMethods.Click(true);
                        break;
                    case BuilderActionKind.RelativeMove:
                        NativeMethods.MoveRelative(action.X, action.Y);
                        break;
                }
            }
        }
        finally
        {
            if (shiftDown) NativeMethods.KeyUp(NativeMethods.VkShift);
            if (controlDown) NativeMethods.KeyUp(NativeMethods.VkControl);
            if (middleDown) NativeMethods.SetMiddleButton(false);
        }
    }

    private static void SetModifier(byte key, bool requested, ref bool current)
    {
        if (requested == current) return;
        if (requested) NativeMethods.KeyDown(key); else NativeMethods.KeyUp(key);
        current = requested;
    }
}

internal static class NativeMethods
{
    public const int SmCxScreen = 0;
    public const int SmCyScreen = 1;
    public const byte VkShift = 0x10;
    public const byte VkControl = 0x11;
    public const byte VkReturn = 0x0D;
    public const byte VkPrior = 0x21;
    private const uint KeyEventKeyUp = 0x0002;
    private const uint MouseLeftDown = 0x0002;
    private const uint MouseLeftUp = 0x0004;
    private const uint MouseRightDown = 0x0008;
    private const uint MouseRightUp = 0x0010;
    private const uint MouseMiddleDown = 0x0020;
    private const uint MouseMiddleUp = 0x0040;
    private const uint MouseMove = 0x0001;

    [DllImport("user32.dll", SetLastError = true)]
    public static extern int GetSystemMetrics(int index);
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")]
    private static extern void keybd_event(byte virtualKey, byte scanCode, uint flags, UIntPtr extraInfo);
    [DllImport("user32.dll")]
    private static extern void mouse_event(uint flags, uint dx, uint dy, uint data, UIntPtr extraInfo);

    public static void KeyDown(byte key) => keybd_event(key, 0, 0, UIntPtr.Zero);
    public static void KeyUp(byte key) => keybd_event(key, 0, KeyEventKeyUp, UIntPtr.Zero);
    public static void PressKey(byte key) { KeyDown(key); KeyUp(key); }
    public static void Click(bool right)
    {
        mouse_event(right ? MouseRightDown : MouseLeftDown, 0, 0, 0, UIntPtr.Zero);
        mouse_event(right ? MouseRightUp : MouseLeftUp, 0, 0, 0, UIntPtr.Zero);
    }
    public static void SetMiddleButton(bool down) =>
        mouse_event(down ? MouseMiddleDown : MouseMiddleUp, 0, 0, 0, UIntPtr.Zero);
    public static void MoveRelative(int x, int y) =>
        mouse_event(MouseMove, unchecked((uint)x), unchecked((uint)y), 0, UIntPtr.Zero);
}

internal readonly record struct GridPoint(int X, int Y);
internal readonly record struct PixelPoint(int X, int Y);
internal readonly record struct PlacedUnit(BuilderUnitDefinition Definition, GridPoint Position);
internal enum BuilderActionKind : byte { Key, Move, Click, RightClick, RelativeMove }
internal readonly record struct BuilderAction(BuilderActionKind Kind, int X, int Y, int KeyCode, int DelayMs, bool Shift, bool Control, bool Middle)
{
    public static BuilderAction Key(char character, int delay, bool control = false)
    {
        var upper = char.ToUpperInvariant(character);
        var key = char.IsAsciiLetterUpper(upper) || char.IsAsciiDigit(upper)
            ? upper
            : character == ' ' ? 0x20 : throw new ArgumentOutOfRangeException(nameof(character));
        return new(BuilderActionKind.Key, 0, 0, key, delay, char.IsLetter(character) && char.IsUpper(character), control, false);
    }
    public static BuilderAction VirtualKey(int key, int delay) => new(BuilderActionKind.Key, 0, 0, key, delay, false, false, false);
    public static BuilderAction Move(PixelPoint point, int delay, bool middle = false) => new(BuilderActionKind.Move, point.X, point.Y, 0, delay, false, false, middle);
    public static BuilderAction RelativeMove(int x, int y, int delay, bool middle = false) => new(BuilderActionKind.RelativeMove, x, y, 0, delay, false, false, middle);
    public static BuilderAction Click(PixelPoint point, int delay) => new(BuilderActionKind.Click, point.X, point.Y, 0, delay, false, false, false);
    public static BuilderAction RightClick(PixelPoint point, int delay) => new(BuilderActionKind.RightClick, point.X, point.Y, 0, delay, false, false, false);
}
