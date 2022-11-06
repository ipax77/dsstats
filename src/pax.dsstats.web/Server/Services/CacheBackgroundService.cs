using pax.dsstats.dbng.Repositories;
using pax.dsstats.dbng.Services;
using System.Diagnostics;

namespace pax.dsstats.web.Server.Services;



public class CacheBackgroundService : IHostedService, IDisposable
{
    private readonly IServiceProvider serviceProvider;
    private readonly ILogger<CacheBackgroundService> logger;
    private Timer? _timer;

    public CacheBackgroundService(IServiceProvider serviceProvider, ILogger<CacheBackgroundService> logger)
    {
        this.serviceProvider = serviceProvider;
        this.logger = logger;
    }

    public Task StartAsync(CancellationToken stoppingToken)
    {
        _timer = new Timer(DoWork, null, new TimeSpan(0, 0, 4), new TimeSpan(1, 0, 0));
        return Task.CompletedTask;
    }

    private async void DoWork(object? state)
    {
        using var scope = serviceProvider.CreateScope();
        var uploadService = scope.ServiceProvider.GetRequiredService<UploadService>();

        Stopwatch sw = Stopwatch.StartNew();

        if (uploadService.WeHaveNewReplays)
        {
            uploadService.WeHaveNewReplays = false;
            var statsService = scope.ServiceProvider.GetRequiredService<IStatsService>();
            statsService.ResetStatsCache();

            await statsService.GetRequestStats(new shared.StatsRequest() { Uploaders = false });
        }

        var replayRepository = scope.ServiceProvider.GetRequiredService<IReplayRepository>();
        await replayRepository.SetReplayViews();

        sw.Stop();
        logger.LogWarning($"{DateTime.UtcNow.ToString(@"yyyy-MM-dd HH:mm:ss")} - Work done in {sw.ElapsedMilliseconds} ms");
    }

    public Task StopAsync(CancellationToken stoppingToken)
    {
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}

