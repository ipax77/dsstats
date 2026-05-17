using dsstats.pwa.Workers;
using dsstats.shared;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using System.Text.Json;

namespace dsstats.pwa.Clients;

/// <summary>
/// Main-thread bridge to the Web Worker pool.
/// Call InitAsync once before the first batch decode, then DecodeAsync per file.
/// </summary>
[SupportedOSPlatform("browser")]
public partial class DecodeClient : IAsyncDisposable
{
    private bool _initialized;

    public async Task InitAsync(int workerCount)
    {
        if (_initialized) return;

        await JSHost.ImportAsync("DecodeClient", "../Clients/DecodeClient.razor.js");
        InitWorkerPool(workerCount);
        await WaitForAllReadyJs();
        _initialized = true;
    }

    /// <summary>
    /// Dispatches replay bytes to an idle worker and returns the decoded replay.
    /// Returns (false, error, null, null) on decode failure.
    /// If <paramref name="cancellationToken"/> fires before the worker responds the
    /// call throws <see cref="OperationCanceledException"/>. The underlying JS
    /// promise stays alive until the worker finishes, then resolves silently —
    /// the pool stays healthy.
    /// </summary>
    public async Task<(bool Success, string? Error, string? Hash, ReplayDto? Replay)> DecodeAsync(
        byte[] bytes, CancellationToken cancellationToken = default)
    {
        // byte[] cannot be marshaled directly in JSImport — encode as base64 string.
        // JS decodeReplay() uses atob() to recover the Uint8Array before dispatching.
        var base64 = Convert.ToBase64String(bytes);
        var json = await DecodeReplayJs(base64).WaitAsync(cancellationToken);
        var result = JsonSerializer.Deserialize(json, WorkerSerializerContext.Default.WorkerDecodeResult);
        if (result is null || !result.Success)
            return (false, result?.Error ?? "null result from worker", null, null);

        var replay = JsonSerializer.Deserialize(result.ReplayJson!,
            WorkerSerializerContext.Default.ReplayDto);
        return (true, null, result.Hash, replay);
    }

    public TimeSpan ConsumeBrowserPause()
        => TimeSpan.FromMilliseconds(ConsumeBrowserPauseMilliseconds());

    public ValueTask DisposeAsync()
    {
        if (_initialized)
        {
            TerminateWorkerPool();
            _initialized = false;
        }
        return ValueTask.CompletedTask;
    }

    // ── JSImport wrappers ──────────────────────────────────────────────────

    [JSImport("initWorkerPool", "DecodeClient")]
    private static partial void InitWorkerPool(int count);

    [JSImport("waitForAllReady", "DecodeClient")]
    private static partial Task WaitForAllReadyJs();

    [JSImport("terminateWorkerPool", "DecodeClient")]
    private static partial void TerminateWorkerPool();

    [JSImport("decodeReplay", "DecodeClient")]
    private static partial Task<string> DecodeReplayJs(string base64Bytes);

    [JSImport("consumeBrowserPauseMilliseconds", "DecodeClient")]
    private static partial double ConsumeBrowserPauseMilliseconds();
}
