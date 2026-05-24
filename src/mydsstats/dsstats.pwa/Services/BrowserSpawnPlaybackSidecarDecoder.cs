using dsstats.shared;
using dsstats.weblib.Replays;
using Microsoft.JSInterop;
using System.IO.Compression;
using System.Reflection;

namespace dsstats.pwa.Services;

public sealed class BrowserSpawnPlaybackSidecarDecoder : ISpawnPlaybackSidecarDecoder, IAsyncDisposable
{
    private static readonly string ModuleVersion =
        typeof(dsstats.indexedDb.Services.IndexedDbService).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion?.Split('+')[0]
        ?? typeof(dsstats.indexedDb.Services.IndexedDbService).Assembly.GetName().Version?.ToString()
        ?? "1.0.0";

    private readonly Task<IJSObjectReference> moduleTask;

    public BrowserSpawnPlaybackSidecarDecoder(IJSRuntime jsRuntime)
    {
        moduleTask = jsRuntime.InvokeAsync<IJSObjectReference>(
            "import",
            $"./_content/dsstats.indexedDb/js/spawn-playback-compression.js?v={Uri.EscapeDataString(ModuleVersion)}").AsTask();
    }

    public async Task<SpawnPlaybackSidecarDto?> DecodeSidecar(
        byte[] payload,
        SpawnPlaybackCompression compression,
        CancellationToken token = default)
    {
        if (payload.Length == 0)
        {
            return null;
        }

        try
        {
            var rawPayload = compression switch
            {
                SpawnPlaybackCompression.Brotli => await DecompressBrotli(payload, token),
                SpawnPlaybackCompression.GZip => DecompressGZip(payload),
                _ => throw new NotSupportedException($"Unsupported spawn playback sidecar compression {compression}.")
            };

            return SpawnPlaybackSidecarCodec.DecodeUncompressed(rawPayload);
        }
        catch (Exception ex) when (ex is InvalidDataException or IOException or NotSupportedException or JSException)
        {
            return null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (moduleTask.IsCompletedSuccessfully)
        {
            await moduleTask.Result.DisposeAsync();
        }
    }

    private async Task<byte[]> DecompressBrotli(byte[] payload, CancellationToken token)
    {
        var module = await moduleTask;
        return await module.InvokeAsync<byte[]>("decompressSpawnPlaybackPayload", token, payload);
    }

    private static byte[] DecompressGZip(byte[] payload)
    {
        using var source = new MemoryStream(payload);
        using var gzip = new GZipStream(source, CompressionMode.Decompress);
        using var raw = new MemoryStream();
        gzip.CopyTo(raw);
        return raw.ToArray();
    }
}
