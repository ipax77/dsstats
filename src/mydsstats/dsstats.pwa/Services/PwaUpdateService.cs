using Microsoft.JSInterop;

namespace dsstats.pwa.Services;

public sealed class PwaUpdateService(
    IJSRuntime jsRuntime,
    ILogger<PwaUpdateService> logger) : IAsyncDisposable
{
    private readonly SemaphoreSlim _initializeLock = new(1, 1);
    private DotNetObjectReference<PwaUpdateService>? _callbacks;
    private IJSObjectReference? _module;
    private bool _initialized;

    public event Action? UpdateAvailableChanged;

    public bool IsUpdateAvailable { get; private set; }
    public bool WasUpdated { get; private set; }

    public async ValueTask InitializeAsync()
    {
        if (_initialized)
        {
            return;
        }

        await _initializeLock.WaitAsync();
        try
        {
            if (_initialized)
            {
                return;
            }

            var version = Uri.EscapeDataString(DecodeService.Version.ToString());
            _module = await jsRuntime.InvokeAsync<IJSObjectReference>("import", $"./js/pwa-update.js?v={version}");
            _callbacks = DotNetObjectReference.Create(this);
            IsUpdateAvailable = await _module.InvokeAsync<bool>("initialize", _callbacks);
            WasUpdated = await _module.InvokeAsync<bool>("consumeAppliedUpdate");
            _initialized = true;
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "PWA update service initialization failed.");
        }
        finally
        {
            _initializeLock.Release();
        }
    }

    public async ValueTask ApplyUpdateAsync()
    {
        await InitializeAsync();

        if (_module is null)
        {
            return;
        }

        await _module.InvokeVoidAsync("applyUpdate");
    }

    [JSInvokable]
    public void SetUpdateAvailable(bool isUpdateAvailable)
    {
        if (IsUpdateAvailable == isUpdateAvailable)
        {
            return;
        }

        IsUpdateAvailable = isUpdateAvailable;
        UpdateAvailableChanged?.Invoke();
    }

    public async ValueTask DisposeAsync()
    {
        if (_module is not null)
        {
            try
            {
                await _module.InvokeVoidAsync("dispose");
                await _module.DisposeAsync();
            }
            catch
            {
                // Ignore teardown races during app shutdown.
            }
        }

        _callbacks?.Dispose();
        _initializeLock.Dispose();
    }
}
