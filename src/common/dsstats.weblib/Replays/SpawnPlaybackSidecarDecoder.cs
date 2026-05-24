using dsstats.shared;

namespace dsstats.weblib.Replays;

public interface ISpawnPlaybackSidecarDecoder
{
    Task<SpawnPlaybackSidecarDto?> DecodeSidecar(
        byte[] payload,
        SpawnPlaybackCompression compression,
        CancellationToken token = default);
}

public sealed class DotNetSpawnPlaybackSidecarDecoder : ISpawnPlaybackSidecarDecoder
{
    public Task<SpawnPlaybackSidecarDto?> DecodeSidecar(
        byte[] payload,
        SpawnPlaybackCompression compression,
        CancellationToken token = default)
    {
        if (token.IsCancellationRequested)
        {
            return Task.FromCanceled<SpawnPlaybackSidecarDto?>(token);
        }

        try
        {
            return Task.FromResult<SpawnPlaybackSidecarDto?>(SpawnPlaybackSidecarCodec.Decode(payload, compression));
        }
        catch (Exception ex) when (ex is InvalidDataException or IOException or NotSupportedException)
        {
            return Task.FromResult<SpawnPlaybackSidecarDto?>(null);
        }
    }
}
