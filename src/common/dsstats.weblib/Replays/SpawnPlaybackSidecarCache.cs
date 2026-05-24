using dsstats.shared;
using dsstats.shared.Interfaces;

namespace dsstats.weblib.Replays;

public sealed class SpawnPlaybackSidecarCache
{
    private const int MaxEntries = 8;
    private readonly ISpawnPlaybackSidecarDecoder sidecarDecoder;
    private readonly Lock cacheLock = new();
    private readonly Queue<CacheKey> order = [];
    private readonly Dictionary<CacheKey, Task<SpawnPlaybackSidecarDto?>> entries = [];

    public SpawnPlaybackSidecarCache(ISpawnPlaybackSidecarDecoder sidecarDecoder)
    {
        this.sidecarDecoder = sidecarDecoder;
    }

    public Task<SpawnPlaybackSidecarDto?> GetSidecar(
        string replayHash,
        SpawnPlaybackCompression compression,
        IReplayRepository replayRepository,
        CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(replayRepository);

        if (string.IsNullOrWhiteSpace(replayHash))
        {
            return Task.FromResult<SpawnPlaybackSidecarDto?>(null);
        }

        if (token.IsCancellationRequested)
        {
            return Task.FromCanceled<SpawnPlaybackSidecarDto?>(token);
        }

        var key = new CacheKey(replayHash, compression);
        lock (cacheLock)
        {
            if (entries.TryGetValue(key, out var cached))
            {
                return cached;
            }

            var task = LoadSidecar(replayHash, compression, replayRepository);
            entries.Add(key, task);
            order.Enqueue(key);
            EvictOverflow();
            return task;
        }
    }

    private void EvictOverflow()
    {
        while (entries.Count > MaxEntries && order.TryDequeue(out var oldest))
        {
            entries.Remove(oldest);
        }
    }

    private async Task<SpawnPlaybackSidecarDto?> LoadSidecar(
        string replayHash,
        SpawnPlaybackCompression compression,
        IReplayRepository replayRepository)
    {
        byte[]? payload = await replayRepository.GetReplaySpawnPlayback(replayHash, CancellationToken.None);
        if (payload is null || payload.Length == 0)
        {
            return null;
        }

        return await sidecarDecoder.DecodeSidecar(payload, compression, CancellationToken.None);
    }

    private readonly record struct CacheKey(string ReplayHash, SpawnPlaybackCompression Compression);
}
