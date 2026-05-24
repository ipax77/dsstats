namespace dsstats.shared;

public readonly record struct SpawnPlaybackBreakpointKey(int GamePos, Breakpoint Breakpoint);

public sealed record SpawnPlaybackProjectedUnit(
    string Name,
    int Count,
    IReadOnlyList<int> Positions);

public static class SpawnPlaybackBreakpointProjector
{
    private const int Min5StartGameloop = 6240;
    private const int Min5EndGameloop = 7209;
    private const int Min10StartGameloop = 12960;
    private const int Min10EndGameloop = 13928;
    private const int Min15StartGameloop = 19680;
    private const int Min15EndGameloop = 20649;

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

            List<SpawnPlaybackUnitSidecarDto> units = [.. player.Units
                .OrderBy(unit => unit.SpawnNumber)
                .ThenBy(unit => unit.SpawnGameloop)
                .ThenBy(unit => unit.UnitIndex)];

            int index = 0;
            while (index < units.Count)
            {
                int spawnNumber = units[index].SpawnNumber;
                int start = index;
                int firstGameloop = units[index].SpawnGameloop;

                while (index < units.Count && units[index].SpawnNumber == spawnNumber)
                {
                    firstGameloop = Math.Min(firstGameloop, units[index].SpawnGameloop);
                    index++;
                }

                var projectedUnits = CreateProjectedUnits(units, start, index);
                var breakpoint = GetBreakpoint(firstGameloop);
                if (breakpoint != Breakpoint.None)
                {
                    result.TryAdd(new(player.GamePos, breakpoint), projectedUnits);
                }

                result[new(player.GamePos, Breakpoint.All)] = projectedUnits;
            }
        }

        return result;
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

            var unitsByName = projectedUnits.ToDictionary(unit => unit.Name, StringComparer.Ordinal);
            foreach (var unit in spawn.Units)
            {
                if (unitsByName.TryGetValue(unit.Name, out var projectedUnit))
                {
                    unit.Positions = [.. projectedUnit.Positions];
                }
            }
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

                var unitsByName = positionSpawn.Units.ToDictionary(unit => unit.Name, StringComparer.Ordinal);
                foreach (var unit in spawn.Units)
                {
                    if (unitsByName.TryGetValue(unit.Name, out var positionedUnit))
                    {
                        unit.Positions = [.. positionedUnit.Positions];
                    }
                }
            }
        }
    }

    private static IReadOnlyList<SpawnPlaybackProjectedUnit> CreateProjectedUnits(
        List<SpawnPlaybackUnitSidecarDto> units,
        int start,
        int end)
    {
        Dictionary<string, List<int>> positionsByName = new(StringComparer.Ordinal);
        for (int i = start; i < end; i++)
        {
            var unit = units[i];
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

    private static Breakpoint GetBreakpoint(int gameloop)
    {
        return gameloop switch
        {
            >= Min5StartGameloop and <= Min5EndGameloop => Breakpoint.Min5,
            >= Min10StartGameloop and <= Min10EndGameloop => Breakpoint.Min10,
            >= Min15StartGameloop and <= Min15EndGameloop => Breakpoint.Min15,
            _ => Breakpoint.None
        };
    }
}
