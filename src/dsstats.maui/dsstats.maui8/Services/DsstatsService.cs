
using dsstats.localization;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace dsstats.maui8.Services;

public partial class DsstatsService
{
    private readonly IServiceScopeFactory scopeFactory;
    private readonly IStringLocalizer<DsstatsLoc> Loc;
    private readonly ILogger<DsstatsService> logger;
    private readonly WatchService watchService;

    public DsstatsService(IServiceScopeFactory scopeFactory, IStringLocalizer<DsstatsLoc> loc, ILogger<DsstatsService> logger)
    {
        this.scopeFactory = scopeFactory;
        Loc = loc;
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
            OnDecodeStateChanged(new() { Info = Loc["Already decoding."] });
            return;
        }
        OnDecodeStateChanged(new() { Info = Loc["New replay detected."] });
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

    public void DEBUGProduceReplayError()
    {
        OnDecodeStateChanged(new()
        {
            DecodeError = new()
            {
                ReplayPath = $"/path/to/my/replay{Random.Shared.Next(1, 10000)}",
                Error = "TestError"
            }
        });
    }
}

public class ScanEventArgs : EventArgs
{
    public int NewReplays { get; init; }
    public int DbReplays { get; init; }
}