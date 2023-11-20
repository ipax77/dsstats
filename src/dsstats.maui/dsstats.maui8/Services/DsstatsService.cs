
using Microsoft.Extensions.Logging;

namespace dsstats.maui8.Services;

public partial class DsstatsService
{
    private readonly IServiceScopeFactory scopeFactory;
    private readonly ILogger<DsstatsService> logger;
    private readonly WatchService watchService;

    public DsstatsService(IServiceScopeFactory scopeFactory, ILogger<DsstatsService> logger)
    {
        this.scopeFactory = scopeFactory;
        this.logger = logger;
        using var scope = scopeFactory.CreateScope();
        var configService = scope.ServiceProvider.GetRequiredService<ConfigService>();
        watchService = new(configService.GetReplayFolders(), configService.AppOptions.ReplayStartName);
        watchService.NewFileDetected += WatchService_NewFileDetected;
        watchService.WatchForNewReplays();
    }

    private void WatchService_NewFileDetected(object? sender, ReplayDetectedEventArgs e)
    {
        if (ctsDecode != null && !ctsDecode.IsCancellationRequested)
        {
            OnDecodeStateChanged(new() { Info = "Already decoding." });
            return;
        }
        OnDecodeStateChanged(new() { Info = "New replay detected." });
        SetupDecodeJob(1);
        _ = StartDecodeJob(new List<string>() { e.Path }).ConfigureAwait(false);
    }

    public int NewReplaysCount { get; private set; }
    public int DbReplaysCount { get; private set; }

    public event EventHandler<ScanEventArgs>? ScanStateChanged;
    protected virtual void OnScanStateChanged(ScanEventArgs e)
    {
        EventHandler<ScanEventArgs>? handler = ScanStateChanged;
        handler?.Invoke(this, e);
    }
}

public class ScanEventArgs : EventArgs
{
    public int NewReplays { get; init; }
    public int DbReplays { get; init; }
}