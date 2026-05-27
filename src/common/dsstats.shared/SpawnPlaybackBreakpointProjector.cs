namespace dsstats.shared;

public readonly record struct SpawnPlaybackBreakpointKey(int GamePos, Breakpoint Breakpoint);

public sealed record SpawnPlaybackProjectedUnit(
    string Name,
    int Count,
    IReadOnlyList<int> Positions);

public static class SpawnPlaybackBreakpointProjector
{
    private const int Min5TargetGameloop = 6_720;
    private const int Min10TargetGameloop = 13_440;
    private const int Min15TargetGameloop = 20_160;

    public static IReadOnlyDictionary<SpawnPlaybackBreakpointKey, IReadOnlyList<SpawnPlaybackProjectedUnit>> Project(
        SpawnPlaybackSidecarDto sidecar)
    {
        ArgumentNullException.ThrowIfNull(sidecar);

        Dictionary<SpawnPlaybackBreakpointKey, IReadOnlyList<SpawnPlaybackProjectedUnit>> result = [];
        var snapshotsByBreakpoint = GetSnapshotsByBreakpoint(sidecar.Snapshots);

        foreach (var player in sidecar.Players)
        {
            if (player.Units.Count == 0)
            {
                continue;
            }

            var unitsBySpawnNumber = player.Units
                .GroupBy(unit => unit.SpawnNumber)
                .ToDictionary(
                    group => group.Key,
                    group => CreateProjectedUnits(group
                        .OrderBy(unit => unit.SpawnGameloop)
                        .ThenBy(unit => unit.UnitIndex)));

            if (snapshotsByBreakpoint.Count > 0)
            {
                foreach (var (breakpoint, snapshot) in snapshotsByBreakpoint)
                {
                    if (unitsBySpawnNumber.TryGetValue(snapshot.SpawnNumber, out var projectedUnits))
                    {
                        result[new(player.GamePos, breakpoint)] = projectedUnits;
                    }
                }

                continue;
            }

            foreach (var (spawnNumber, projectedUnits) in unitsBySpawnNumber.OrderBy(x => x.Key))
            {
                var firstGameloop = player.Units
                    .Where(unit => unit.SpawnNumber == spawnNumber)
                    .Min(unit => unit.SpawnGameloop);
                var breakpoint = GetFallbackBreakpoint(firstGameloop);
                if (breakpoint != Breakpoint.None)
                {
                    result.TryAdd(new(player.GamePos, breakpoint), projectedUnits);
                }

                result[new(player.GamePos, Breakpoint.All)] = projectedUnits;
            }
        }

        return result;
    }

    public static bool ApplyToSpawn(
        ReplayDto replay,
        SpawnPlaybackBreakpointKey key,
        IReadOnlyDictionary<SpawnPlaybackBreakpointKey, IReadOnlyList<SpawnPlaybackProjectedUnit>> projected)
    {
        ArgumentNullException.ThrowIfNull(replay);
        ArgumentNullException.ThrowIfNull(projected);

        if (!projected.TryGetValue(key, out var projectedUnits))
        {
            return false;
        }

        var player = replay.Players.FirstOrDefault(player => player.GamePos == key.GamePos);
        var spawn = player?.Spawns.FirstOrDefault(spawn => spawn.Breakpoint == key.Breakpoint);
        if (spawn is null)
        {
            return false;
        }

        ApplyUnits(spawn, projectedUnits);
        return true;
    }

    public static bool ApplyToSpawn(
        ReplayDto replay,
        SpawnPlaybackBreakpointKey key,
        ReplaySpawnPositionsDto positions)
    {
        ArgumentNullException.ThrowIfNull(replay);
        ArgumentNullException.ThrowIfNull(positions);

        var positionSpawn = positions.Players
            .FirstOrDefault(player => player.GamePos == key.GamePos)?
            .Spawns
            .FirstOrDefault(spawn => spawn.Breakpoint == key.Breakpoint);

        if (positionSpawn is null)
        {
            return false;
        }

        var player = replay.Players.FirstOrDefault(player => player.GamePos == key.GamePos);
        var spawn = player?.Spawns.FirstOrDefault(spawn => spawn.Breakpoint == key.Breakpoint);
        if (spawn is null)
        {
            return false;
        }

        ApplyUnits(spawn, positionSpawn.Units);
        return true;
    }

    public static void ApplyToReplay(ReplayDto replay, SpawnPlaybackSidecarDto sidecar)
    {
        ArgumentNullException.ThrowIfNull(replay);
        ArgumentNullException.ThrowIfNull(sidecar);

        var projected = Project(sidecar);
        var playersByGamePos = replay.Players.ToDictionary(player => player.GamePos);

        foreach (var (key, projectedUnits) in projected)
        {
            if (!playersByGamePos.TryGetValue(key.GamePos, out var player))
            {
                continue;
            }

            var spawn = player.Spawns.FirstOrDefault(spawn => spawn.Breakpoint == key.Breakpoint);
            if (spawn is null)
            {
                continue;
            }

            ApplyUnits(spawn, projectedUnits);
        }
    }

    public static void ApplyToReplay(ReplayDto replay, ReplaySpawnPositionsDto positions)
    {
        ArgumentNullException.ThrowIfNull(replay);
        ArgumentNullException.ThrowIfNull(positions);

        var playersByGamePos = replay.Players.ToDictionary(player => player.GamePos);
        foreach (var positionPlayer in positions.Players)
        {
            if (!playersByGamePos.TryGetValue(positionPlayer.GamePos, out var player))
            {
                continue;
            }

            var spawnsByBreakpoint = player.Spawns.ToDictionary(spawn => spawn.Breakpoint);
            foreach (var positionSpawn in positionPlayer.Spawns)
            {
                if (!spawnsByBreakpoint.TryGetValue(positionSpawn.Breakpoint, out var spawn))
                {
                    continue;
                }

                ApplyUnits(spawn, positionSpawn.Units);
            }
        }
    }

    private static IReadOnlyList<SpawnPlaybackProjectedUnit> CreateProjectedUnits(
        IEnumerable<SpawnPlaybackUnitSidecarDto> units)
    {
        Dictionary<string, List<int>> positionsByName = new(StringComparer.Ordinal);
        foreach (var unit in units)
        {
            if (!positionsByName.TryGetValue(unit.Name, out var positions))
            {
                positions = [];
                positionsByName[unit.Name] = positions;
            }

            positions.Add(unit.SpawnX);
            positions.Add(unit.SpawnY);
        }

        List<SpawnPlaybackProjectedUnit> projected = new(positionsByName.Count);
        foreach (var (name, positions) in positionsByName)
        {
            projected.Add(new(name, positions.Count / 2, positions));
        }

        return projected;
    }

    private static Dictionary<Breakpoint, SpawnPlaybackSnapshotSidecarDto> GetSnapshotsByBreakpoint(
        IReadOnlyList<SpawnPlaybackSnapshotSidecarDto> snapshots)
    {
        if (snapshots.Count == 0)
        {
            return [];
        }

        Dictionary<Breakpoint, SpawnPlaybackSnapshotSidecarDto> result = [];
        AddClosest(result, Breakpoint.Min5, snapshots, Min5TargetGameloop);
        AddClosest(result, Breakpoint.Min10, snapshots, Min10TargetGameloop);
        AddClosest(result, Breakpoint.Min15, snapshots, Min15TargetGameloop);

        var latest = snapshots
            .OrderByDescending(snapshot => snapshot.EndGameloop)
            .ThenByDescending(snapshot => snapshot.SpawnNumber)
            .First();
        result[Breakpoint.All] = latest;

        return result;
    }

    private static void AddClosest(
        Dictionary<Breakpoint, SpawnPlaybackSnapshotSidecarDto> result,
        Breakpoint breakpoint,
        IReadOnlyList<SpawnPlaybackSnapshotSidecarDto> snapshots,
        int targetGameloop)
    {
        var closest = snapshots
            .OrderBy(snapshot => Math.Abs(snapshot.EndGameloop - targetGameloop))
            .ThenBy(snapshot => snapshot.EndGameloop)
            .First();

        result[breakpoint] = closest;
    }

    private static Breakpoint GetFallbackBreakpoint(int gameloop)
    {
        return gameloop switch
        {
            >= 6_240 and <= 7_209 => Breakpoint.Min5,
            >= 12_960 and <= 13_928 => Breakpoint.Min10,
            >= 19_680 and <= 20_649 => Breakpoint.Min15,
            _ => Breakpoint.None
        };
    }

    private static void ApplyUnits(SpawnDto spawn, IReadOnlyList<SpawnPlaybackProjectedUnit> projectedUnits)
    {
        var unitsByName = projectedUnits.ToDictionary(unit => unit.Name, StringComparer.Ordinal);
        foreach (var unit in spawn.Units)
        {
            if (unitsByName.TryGetValue(unit.Name, out var projectedUnit))
            {
                unit.Positions = [.. projectedUnit.Positions];
            }
        }
    }

    private static void ApplyUnits(SpawnDto spawn, IReadOnlyList<UnitPositionsDto> positionedUnits)
    {
        var unitsByName = positionedUnits.ToDictionary(unit => unit.Name, StringComparer.Ordinal);
        foreach (var unit in spawn.Units)
        {
            if (unitsByName.TryGetValue(unit.Name, out var positionedUnit))
            {
                unit.Positions = [.. positionedUnit.Positions];
            }
        }
    }
}
