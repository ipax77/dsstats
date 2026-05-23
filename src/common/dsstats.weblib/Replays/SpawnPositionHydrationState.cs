using dsstats.shared;
using dsstats.shared.Interfaces;

namespace dsstats.weblib.Replays;

public sealed class SpawnPositionHydrationState
{
    private readonly Lock hydrationLock = new();
    private string? hydrationReplayHash;
    private Task<bool>? hydrationTask;
    private string? sidecarReplayHash;
    private Task<SpawnPlaybackSidecarDto?>? sidecarTask;

    public Task<bool> EnsureHydrated(
        ReplayDetails replayDetails,
        IReplayRepository replayRepository,
        CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(replayDetails);
        ArgumentNullException.ThrowIfNull(replayRepository);

        if (!NeedsHydration(replayDetails.Replay))
        {
            return Task.FromResult(false);
        }

        lock (hydrationLock)
        {
            if (hydrationReplayHash == replayDetails.ReplayHash && hydrationTask is not null)
            {
                return hydrationTask;
            }

            hydrationReplayHash = replayDetails.ReplayHash;
            hydrationTask = Hydrate(replayDetails, replayRepository, token);
            return hydrationTask;
        }
    }

    public Task<SpawnPlaybackSidecarDto?> GetSidecar(
        ReplayDetails replayDetails,
        IReplayRepository replayRepository,
        CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(replayDetails);
        ArgumentNullException.ThrowIfNull(replayRepository);

        if (replayDetails.Replay.SpawnPlayback?.Available != true)
        {
            return Task.FromResult<SpawnPlaybackSidecarDto?>(null);
        }

        lock (hydrationLock)
        {
            if (sidecarReplayHash == replayDetails.ReplayHash && sidecarTask is not null)
            {
                return sidecarTask;
            }

            sidecarReplayHash = replayDetails.ReplayHash;
            sidecarTask = LoadSidecar(replayDetails.ReplayHash, replayRepository, token);
            return sidecarTask;
        }
    }

    public static bool NeedsHydration(ReplayDto replay)
    {
        if (replay.SpawnPlayback?.Available != true)
        {
            return false;
        }

        return replay.Players
            .SelectMany(player => player.Spawns)
            .SelectMany(spawn => spawn.Units)
            .Any(unit => unit.Count > 0 && unit.Positions is null);
    }

    private static async Task<bool> Hydrate(
        ReplayDetails replayDetails,
        IReplayRepository replayRepository,
        CancellationToken token)
    {
        var sidecar = await LoadSidecar(replayDetails.ReplayHash, replayRepository, token);
        if (sidecar is not null)
        {
            SpawnPlaybackBreakpointProjector.ApplyToReplay(replayDetails.Replay, sidecar);
            return true;
        }

        var positions = await replayRepository.GetReplaySpawnPositions(replayDetails.ReplayHash, token);
        if (positions is null)
        {
            return false;
        }

        SpawnPlaybackBreakpointProjector.ApplyToReplay(replayDetails.Replay, positions);
        return true;
    }

    private static async Task<SpawnPlaybackSidecarDto?> LoadSidecar(
        string replayHash,
        IReplayRepository replayRepository,
        CancellationToken token)
    {
        byte[]? payload = await replayRepository.GetReplaySpawnPlayback(replayHash, token);
        if (payload is null || payload.Length == 0)
        {
            return null;
        }

        try
        {
            return SpawnPlaybackSidecarCodec.Decode(payload);
        }
        catch (Exception ex) when (ex is InvalidDataException or IOException or NotSupportedException)
        {
            return null;
        }
    }
}
