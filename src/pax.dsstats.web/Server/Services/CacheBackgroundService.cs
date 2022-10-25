using pax.dsstats.dbng.Services;

namespace pax.dsstats.web.Server.Services;



public class CacheBackgroundService : IHostedService, IDisposable
{
    private readonly IServiceProvider serviceProvider;
    private Timer? _timer;

    public CacheBackgroundService(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }

    public Task StartAsync(CancellationToken stoppingToken)
    {
        _timer = new Timer(DoWork, null, new TimeSpan(0, 0, 4), new TimeSpan(1, 0, 0));
        return Task.CompletedTask;
    }

    private void DoWork(object? state)
    {
        using var scope = serviceProvider.CreateScope();
        var uploadService = scope.ServiceProvider.GetRequiredService<UploadService>();

        if (uploadService.WeHaveNewReplays)
        {
            uploadService.WeHaveNewReplays = false;
            var statsService = scope.ServiceProvider.GetRequiredService<IStatsService>();
            statsService.ResetCache();

            var mmrService = scope.ServiceProvider.GetRequiredService<MmrService>();
            _ = mmrService.CalcMmmr();
        }
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

