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
    private const int SnapshotStartMatchWindowGameloops = 112;

    public static IReadOnlyDictionary<SpawnPlaybackBreakpointKey, IReadOnlyList<SpawnPlaybackProjectedUnit>> Project(
        SpawnPlaybackSidecarDto sidecar)
    {
        ArgumentNullException.ThrowIfNull(sidecar);

        Dictionary<SpawnPlaybackBreakpointKey, IReadOnlyList<SpawnPlaybackProjectedUnit>> result = [];
        foreach (var player in sidecar.Players)
        {
            if (player.Units.Count == 0)
            {
                continue;
            }

            List<SpawnProjection> spawnProjections = [.. player.Units
                .GroupBy(unit => unit.SpawnNumber)
                .Select(group => new SpawnProjection(
                    group.Key,
                    group.Min(unit => unit.SpawnGameloop),
                    CreateProjectedUnits(group
                        .OrderBy(unit => unit.SpawnGameloop)
                        .ThenBy(unit => unit.UnitIndex))))
                .OrderBy(projection => projection.SpawnNumber)];

            if (spawnProjections.Count == 0)
            {
                continue;
            }

            AddProjectedSpawn(result, player.GamePos, Breakpoint.Min5, spawnProjections, sidecar.Snapshots, Min5TargetGameloop);
            AddProjectedSpawn(result, player.GamePos, Breakpoint.Min10, spawnProjections, sidecar.Snapshots, Min10TargetGameloop);
            AddProjectedSpawn(result, player.GamePos, Breakpoint.Min15, spawnProjections, sidecar.Snapshots, Min15TargetGameloop);

            result[new(player.GamePos, Breakpoint.All)] = spawnProjections[^1].Units;

            if (sidecar.Snapshots.Count > 0)
            {
                continue;
            }

            foreach (var projection in spawnProjections)
            {
                var breakpoint = GetFallbackBreakpoint(projection.FirstGameloop);
                if (breakpoint != Breakpoint.None)
                {
                    result.TryAdd(new(player.GamePos, breakpoint), projection.Units);
                }
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

    private static void AddProjectedSpawn(
        Dictionary<SpawnPlaybackBreakpointKey, IReadOnlyList<SpawnPlaybackProjectedUnit>> result,
        int gamePos,
        Breakpoint breakpoint,
        List<SpawnProjection> spawnProjections,
        IReadOnlyList<SpawnPlaybackSnapshotSidecarDto> snapshots,
        int targetGameloop)
    {
        SpawnProjection? bestProjection = null;
        int bestEndGameloop = 0;
        int bestTargetDistance = int.MaxValue;
        foreach (var projection in spawnProjections)
        {
            int endGameloop = GetClosestEndGameloop(projection, snapshots, targetGameloop);
            int targetDistance = Math.Abs(endGameloop - targetGameloop);
            if (bestProjection is null
                || targetDistance < bestTargetDistance
                || (targetDistance == bestTargetDistance && endGameloop < bestEndGameloop)
                || (targetDistance == bestTargetDistance
                    && endGameloop == bestEndGameloop
                    && projection.SpawnNumber < bestProjection.SpawnNumber))
            {
                bestProjection = projection;
                bestEndGameloop = endGameloop;
                bestTargetDistance = targetDistance;
            }
        }

        if (bestProjection is not null)
        {
            result[new(gamePos, breakpoint)] = bestProjection.Units;
        }
    }

    private static int GetClosestEndGameloop(
        SpawnProjection projection,
        IReadOnlyList<SpawnPlaybackSnapshotSidecarDto> snapshots,
        int targetGameloop)
    {
        if (snapshots.Count == 0)
        {
            return projection.FirstGameloop;
        }

        SpawnPlaybackSnapshotSidecarDto? bestSnapshot = null;
        int bestWindowPenalty = int.MaxValue;
        int bestStartDistance = int.MaxValue;
        int bestSpawnNumberPenalty = int.MaxValue;
        int bestTargetDistance = int.MaxValue;

        foreach (var snapshot in snapshots)
        {
            int startDistance = Math.Abs(snapshot.StartGameloop - projection.FirstGameloop);
            int windowPenalty = startDistance <= SnapshotStartMatchWindowGameloops ? 0 : 1;
            int spawnNumberPenalty = snapshot.SpawnNumber == projection.SpawnNumber ? 0 : 1;
            int targetDistance = Math.Abs(snapshot.EndGameloop - targetGameloop);

            if (bestSnapshot is null
                || windowPenalty < bestWindowPenalty
                || (windowPenalty == bestWindowPenalty && startDistance < bestStartDistance)
                || (windowPenalty == bestWindowPenalty
                    && startDistance == bestStartDistance
                    && spawnNumberPenalty < bestSpawnNumberPenalty)
                || (windowPenalty == bestWindowPenalty
                    && startDistance == bestStartDistance
                    && spawnNumberPenalty == bestSpawnNumberPenalty
                    && targetDistance < bestTargetDistance)
                || (windowPenalty == bestWindowPenalty
                    && startDistance == bestStartDistance
                    && spawnNumberPenalty == bestSpawnNumberPenalty
                    && targetDistance == bestTargetDistance
                    && snapshot.EndGameloop < bestSnapshot.EndGameloop))
            {
                bestSnapshot = snapshot;
                bestWindowPenalty = windowPenalty;
                bestStartDistance = startDistance;
                bestSpawnNumberPenalty = spawnNumberPenalty;
                bestTargetDistance = targetDistance;
            }
        }

        return bestSnapshot?.EndGameloop ?? projection.FirstGameloop;
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

    private sealed record SpawnProjection(
        int SpawnNumber,
        int FirstGameloop,
        IReadOnlyList<SpawnPlaybackProjectedUnit> Units);
}
