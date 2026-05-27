using dsstats.shared;
using dsstats.shared.Interfaces;

namespace dsstats.weblib.Replays;

public sealed class SpawnPositionHydrationService
{
    private const int MaxEntries = 8;
    private readonly SpawnPlaybackSidecarCache sidecarCache;
    private readonly Lock cacheLock = new();
    private readonly Queue<string> order = [];
    private readonly Dictionary<string, ReplayHydrationEntry> entries = [];

    public SpawnPositionHydrationService(SpawnPlaybackSidecarCache sidecarCache)
    {
        this.sidecarCache = sidecarCache;
    }

    public async Task<SpawnDto?> EnsureChartSpawnAsync(
        ReplayDetails replayDetails,
        int gamePos,
        Breakpoint breakpoint,
        IReplayRepository replayRepository,
        CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(replayDetails);
        ArgumentNullException.ThrowIfNull(replayRepository);

        var spawn = GetSpawn(replayDetails.Replay, gamePos, breakpoint);
        if (spawn is not null && HasCompleteChartPositions(spawn))
        {
            return spawn;
        }

        token.ThrowIfCancellationRequested();
        var key = new SpawnPlaybackBreakpointKey(gamePos, breakpoint);
        var entry = GetEntry(replayDetails.ReplayHash);
        var projected = await GetProjectedPositions(entry, replayDetails, replayRepository).ConfigureAwait(false);
        if (projected is not null)
        {
            if (spawn is not null)
            {
                SpawnPlaybackBreakpointProjector.ApplyToSpawn(replayDetails.Replay, key, projected);
            }
            else
            {
                spawn = CreateChartSpawn(key, projected);
            }

            if (spawn is not null && HasCompleteChartPositions(spawn))
            {
                return spawn;
            }
        }

        token.ThrowIfCancellationRequested();
        var fallback = await GetFallbackPositions(entry, replayDetails.ReplayHash, replayRepository).ConfigureAwait(false);
        if (fallback is not null)
        {
            if (spawn is not null)
            {
                SpawnPlaybackBreakpointProjector.ApplyToSpawn(replayDetails.Replay, key, fallback);
            }
            else
            {
                spawn = CreateChartSpawn(key, fallback);
            }
        }

        return spawn;
    }

    public static bool HasChartPositions(SpawnDto spawn)
    {
        return spawn.Units
            .Where(unit => unit.Count > 0)
            .Any(unit => unit.Positions is { Count: > 0 });
    }

    public static bool HasCompleteChartPositions(SpawnDto spawn)
    {
        return spawn.Units
            .Where(unit => unit.Count > 0)
            .All(unit => unit.Positions is { Count: > 0 });
    }

    public static SpawnDto? GetSpawn(ReplayDto replay, int gamePos, Breakpoint breakpoint)
    {
        return replay.Players
            .FirstOrDefault(player => player.GamePos == gamePos)?
            .Spawns
            .FirstOrDefault(spawn => spawn.Breakpoint == breakpoint);
    }

    private static SpawnDto? CreateChartSpawn(
        SpawnPlaybackBreakpointKey key,
        IReadOnlyDictionary<SpawnPlaybackBreakpointKey, IReadOnlyList<SpawnPlaybackProjectedUnit>> projected)
    {
        if (!projected.TryGetValue(key, out var projectedUnits))
        {
            return null;
        }

        return new()
        {
            Breakpoint = key.Breakpoint,
            Units = projectedUnits
                .Where(unit => unit.Count > 0 && unit.Positions.Count > 0)
                .Select(unit => new UnitDto
                {
                    Name = unit.Name,
                    Count = unit.Count,
                    Positions = [.. unit.Positions]
                })
                .ToList()
        };
    }

    private static SpawnDto? CreateChartSpawn(SpawnPlaybackBreakpointKey key, ReplaySpawnPositionsDto positions)
    {
        var positionSpawn = positions.Players
            .FirstOrDefault(player => player.GamePos == key.GamePos)?
            .Spawns
            .FirstOrDefault(spawn => spawn.Breakpoint == key.Breakpoint);

        if (positionSpawn is null)
        {
            return null;
        }

        return new()
        {
            Breakpoint = key.Breakpoint,
            Units = positionSpawn.Units
                .Where(unit => unit.Positions.Count > 0)
                .Select(unit => new UnitDto
                {
                    Name = unit.Name,
                    Count = unit.Positions.Count / 2,
                    Positions = [.. unit.Positions]
                })
                .ToList()
        };
    }

    private ReplayHydrationEntry GetEntry(string replayHash)
    {
        lock (cacheLock)
        {
            if (entries.TryGetValue(replayHash, out var entry))
            {
                return entry;
            }

            entry = new();
            entries.Add(replayHash, entry);
            order.Enqueue(replayHash);
            EvictOverflow();
            return entry;
        }
    }

    private void EvictOverflow()
    {
        while (entries.Count > MaxEntries && order.TryDequeue(out var oldest))
        {
            entries.Remove(oldest);
        }
    }

    private Task<IReadOnlyDictionary<SpawnPlaybackBreakpointKey, IReadOnlyList<SpawnPlaybackProjectedUnit>>?> GetProjectedPositions(
        ReplayHydrationEntry entry,
        ReplayDetails replayDetails,
        IReplayRepository replayRepository)
    {
        lock (entry.Sync)
        {
            entry.ProjectedTask ??= LoadProjectedPositions(replayDetails, replayRepository);
            return entry.ProjectedTask;
        }
    }

    private async Task<IReadOnlyDictionary<SpawnPlaybackBreakpointKey, IReadOnlyList<SpawnPlaybackProjectedUnit>>?> LoadProjectedPositions(
        ReplayDetails replayDetails,
        IReplayRepository replayRepository)
    {
        if (replayDetails.Replay.SpawnPlayback?.Available != true)
        {
            return null;
        }

        var sidecar = await sidecarCache.GetSidecar(
            replayDetails.ReplayHash,
            replayDetails.Replay.SpawnPlayback.Compression,
            replayRepository,
            CancellationToken.None).ConfigureAwait(false);

        return sidecar is null
            ? null
            : SpawnPlaybackBreakpointProjector.Project(sidecar);
    }

    private Task<ReplaySpawnPositionsDto?> GetFallbackPositions(
        ReplayHydrationEntry entry,
        string replayHash,
        IReplayRepository replayRepository)
    {
        lock (entry.Sync)
        {
            entry.FallbackTask ??= replayRepository.GetReplaySpawnPositions(replayHash, CancellationToken.None);
            return entry.FallbackTask;
        }
    }

    private sealed class ReplayHydrationEntry
    {
        public Lock Sync { get; } = new();
        public Task<IReadOnlyDictionary<SpawnPlaybackBreakpointKey, IReadOnlyList<SpawnPlaybackProjectedUnit>>?>? ProjectedTask { get; set; }
        public Task<ReplaySpawnPositionsDto?>? FallbackTask { get; set; }
    }
}
