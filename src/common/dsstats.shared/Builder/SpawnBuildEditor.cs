namespace dsstats.shared.Builder;

public enum BuildLayer : byte { Ground, Air }

public readonly record struct BuildCell(int X, int Y)
{
    public bool IsOnBoard => (uint)X < SpawnBuilderFen.Width && (uint)Y < SpawnBuilderFen.Height;
}

public sealed record PlacedBuildUnit(int Id, BuilderUnitDefinition Definition, BuildCell Cell)
{
    public BuildLayer Layer => Definition.IsAir ? BuildLayer.Air : BuildLayer.Ground;
}

public interface IBuildPlacementRule
{
    string? Validate(SpawnBuildEditor editor, BuilderUnitDefinition unit, BuildCell cell, int ignoredUnitId);
}

public interface IBuildEditCommand { bool Execute(SpawnBuildEditor editor, out string? error); }

public sealed record AddUnit(BuilderUnitDefinition Unit, BuildCell Cell) : IBuildEditCommand
{
    public bool Execute(SpawnBuildEditor editor, out string? error) => editor.TryAdd(Unit, Cell, out _, out error);
}

public sealed record RemoveUnits(IReadOnlyCollection<int> UnitIds) : IBuildEditCommand
{
    public bool Execute(SpawnBuildEditor editor, out string? error) { editor.Remove(UnitIds); error = null; return true; }
}

public sealed record MoveUnits(IReadOnlyCollection<int> UnitIds, int DeltaX, int DeltaY) : IBuildEditCommand
{
    public bool Execute(SpawnBuildEditor editor, out string? error) => editor.TryMove(UnitIds, DeltaX, DeltaY, out error);
}

public sealed record ReplaceUnitType(IReadOnlyCollection<int> UnitIds, BuilderUnitDefinition Unit) : IBuildEditCommand
{
    public bool Execute(SpawnBuildEditor editor, out string? error) => editor.TryReplace(UnitIds, Unit, out error);
}

public sealed record ClearBuild(BuildLayer? Layer = null) : IBuildEditCommand
{
    public bool Execute(SpawnBuildEditor editor, out string? error) { editor.Clear(Layer); error = null; return true; }
}

public sealed record PasteUnits(IReadOnlyList<PlacedBuildUnit> Units) : IBuildEditCommand
{
    public bool Execute(SpawnBuildEditor editor, out string? error) => editor.TryPaste(Units, out error);
}

public sealed class SpawnBuildEditor
{
    private readonly List<PlacedBuildUnit> units = [];
    private readonly Dictionary<int, PlacedBuildUnit> byId = [];
    private readonly int[] occupied = new int[SpawnBuilderFen.Width * SpawnBuilderFen.Height * 2];
    private readonly IReadOnlyList<IBuildPlacementRule> rules;
    private int nextId = 1;

    public SpawnBuildEditor(Commander commander, int team, IEnumerable<IBuildPlacementRule>? rules = null)
    {
        if (team is not (1 or 2)) throw new ArgumentOutOfRangeException(nameof(team));
        Commander = commander;
        Team = team;
        this.rules = rules?.ToArray() ?? [];
    }

    public Commander Commander { get; }
    public int Team { get; }
    public IReadOnlyList<PlacedBuildUnit> Units => units;

    public static SpawnBuildEditor From(SpawnBuilderFenResult fen, IEnumerable<IBuildPlacementRule>? rules = null)
    {
        var editor = new SpawnBuildEditor(fen.Commander, fen.Team, rules);
        foreach (var group in fen.Spawn.Units)
        {
            if (!BuilderUnitCatalog.TryGetUnit(fen.Commander, group.Name, out var definition) || group.Positions is null) continue;
            for (var i = 0; i + 1 < group.Positions.Count; i += 2)
                if (SpawnBuilderFen.TryGetCell(fen.Team, group.Positions[i], group.Positions[i + 1], out var cell))
                    editor.TryAdd(definition, new(cell.X, cell.Y), out _, out _);
        }
        return editor;
    }

    public bool Execute(IBuildEditCommand command, out string? error) => command.Execute(this, out error);

    public bool TryAdd(BuilderUnitDefinition definition, BuildCell cell, out int id, out string? error)
    {
        id = 0;
        if (!Validate(definition, cell, 0, out error)) return false;
        var unit = new PlacedBuildUnit(nextId++, definition, cell);
        AddCore(unit); id = unit.Id; return true;
    }

    public void Remove(IEnumerable<int> ids)
    {
        foreach (var id in ids.ToArray()) if (byId.Remove(id, out var unit)) { Mark(unit, 0); units.Remove(unit); }
    }

    public bool TryMove(IReadOnlyCollection<int> ids, int dx, int dy, out string? error)
    {
        var moving = ids.Select(id => byId.GetValueOrDefault(id)).OfType<PlacedBuildUnit>().ToArray();
        foreach (var unit in moving) Mark(unit, 0);
        List<PlacedBuildUnit> candidates = new(moving.Length);
        foreach (var unit in moving)
        {
            var candidate = unit with { Cell = new(unit.Cell.X + dx, unit.Cell.Y + dy) };
            if (!Validate(candidate.Definition, candidate.Cell, unit.Id, out error))
            { foreach (var clear in candidates) Mark(clear, 0); foreach (var restore in moving) Mark(restore, restore.Id); return false; }
            candidates.Add(candidate); Mark(candidate, candidate.Id);
        }
        foreach (var unit in moving) { units.Remove(unit); byId.Remove(unit.Id); }
        foreach (var candidate in candidates) AddCore(candidate);
        error = null; return true;
    }

    public bool TryReplace(IReadOnlyCollection<int> ids, BuilderUnitDefinition definition, out string? error)
    {
        var replacing = ids.Select(id => byId.GetValueOrDefault(id)).OfType<PlacedBuildUnit>().ToArray();
        foreach (var unit in replacing) Mark(unit, 0);
        List<PlacedBuildUnit> candidates = new(replacing.Length);
        foreach (var unit in replacing)
        {
            var candidate = unit with { Definition = definition };
            if (!Validate(definition, unit.Cell, unit.Id, out error))
            { foreach (var clear in candidates) Mark(clear, 0); foreach (var restore in replacing) Mark(restore, restore.Id); return false; }
            candidates.Add(candidate); Mark(candidate, candidate.Id);
        }
        foreach (var unit in replacing) { units.Remove(unit); byId.Remove(unit.Id); }
        foreach (var candidate in candidates) AddCore(candidate);
        error = null; return true;
    }

    public bool TryPaste(IReadOnlyList<PlacedBuildUnit> pasted, out string? error)
    {
        List<int> added = new(pasted.Count);
        foreach (var unit in pasted)
            if (TryAdd(unit.Definition, unit.Cell, out var id, out error)) added.Add(id);
            else { Remove(added); return false; }
        error = null; return true;
    }

    public void Clear(BuildLayer? layer)
    {
        var removed = layer is null ? units.ToArray() : units.Where(u => u.Layer == layer).ToArray();
        Remove(removed.Select(u => u.Id));
    }

    public SpawnDto ToSpawn()
    {
        var result = new SpawnDto();
        foreach (var group in units.GroupBy(u => u.Definition.Name))
        {
            var positions = new List<int>(group.Count() * 2);
            foreach (var unit in group)
                if (SpawnBuilderFen.TryGetMapPosition(Team, new(unit.Cell.X, unit.Cell.Y), out var x, out var y)) { positions.Add(x); positions.Add(y); }
            result.Units.Add(new() { Name = group.Key, Count = positions.Count / 2, Positions = positions });
        }
        return result;
    }

    private bool Validate(BuilderUnitDefinition definition, BuildCell cell, int ignoredId, out string? error)
    {
        var size = definition.Footprint;
        if (!cell.IsOnBoard || cell.X + size > SpawnBuilderFen.Width || cell.Y + size > SpawnBuilderFen.Height)
        { error = "The unit footprint is outside the build area."; return false; }
        var offset = definition.IsAir ? SpawnBuilderFen.Width * SpawnBuilderFen.Height : 0;
        for (var y = cell.Y; y < cell.Y + size; y++) for (var x = cell.X; x < cell.X + size; x++)
        { var occupant = occupied[offset + y * SpawnBuilderFen.Width + x]; if (occupant != 0 && occupant != ignoredId) { error = "The unit footprint collides with another unit on this layer."; return false; } }
        foreach (var rule in rules) if ((error = rule.Validate(this, definition, cell, ignoredId)) is not null) return false;
        error = null; return true;
    }

    private void AddCore(PlacedBuildUnit unit) { units.Add(unit); byId.Add(unit.Id, unit); Mark(unit, unit.Id); }
    private void Mark(PlacedBuildUnit unit, int value)
    {
        var offset = unit.Definition.IsAir ? SpawnBuilderFen.Width * SpawnBuilderFen.Height : 0;
        for (var y = unit.Cell.Y; y < unit.Cell.Y + unit.Definition.Footprint; y++) for (var x = unit.Cell.X; x < unit.Cell.X + unit.Definition.Footprint; x++) occupied[offset + y * SpawnBuilderFen.Width + x] = value;
    }
}
