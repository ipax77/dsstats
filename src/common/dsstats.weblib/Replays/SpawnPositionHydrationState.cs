using dsstats.shared;
using dsstats.shared.Interfaces;

namespace dsstats.weblib.Replays;

public sealed class SpawnPositionHydrationState
{
    private readonly Lock hydrationLock = new();
    private string? sidecarReplayHash;
    private Task<SpawnPlaybackSidecarDto?>? sidecarTask;

    public Task<SpawnPlaybackSidecarDto?> GetSidecar(
        ReplayDetails replayDetails,
        SpawnPlaybackSidecarCache sidecarCache,
        IReplayRepository replayRepository,
        CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(replayDetails);
        ArgumentNullException.ThrowIfNull(sidecarCache);
        ArgumentNullException.ThrowIfNull(replayRepository);

        if (replayDetails.Replay.SpawnPlayback?.Available != true)
        {
            return Task.FromResult<SpawnPlaybackSidecarDto?>(null);
        }

        lock (hydrationLock)
        {
            return GetOrLoadSidecar(
                replayDetails.ReplayHash,
                replayDetails.Replay.SpawnPlayback.Compression,
                sidecarCache,
                replayRepository,
                token);
        }
    }

    private Task<SpawnPlaybackSidecarDto?> GetOrLoadSidecar(
        string replayHash,
        SpawnPlaybackCompression compression,
        SpawnPlaybackSidecarCache sidecarCache,
        IReplayRepository replayRepository,
        CancellationToken token)
    {
        if (sidecarReplayHash == replayHash && sidecarTask is not null)
        {
            return sidecarTask;
        }

        sidecarReplayHash = replayHash;
        sidecarTask = sidecarCache.GetSidecar(replayHash, compression, replayRepository, token);
        return sidecarTask;
    }
}
