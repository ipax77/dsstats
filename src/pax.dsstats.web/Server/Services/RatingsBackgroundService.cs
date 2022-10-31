using pax.dsstats.dbng.Services;

namespace pax.dsstats.web.Server.Services;



public class RatingsBackgroundService : IHostedService, IDisposable
{
    private readonly IServiceProvider serviceProvider;
    private Timer? _timer;

    public RatingsBackgroundService(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }

    public Task StartAsync(CancellationToken stoppingToken)
    {
        // run every day a 3 am utc
        DateTime nowTime = DateTime.UtcNow;
        DateTime startTime = DateTime.Today.AddHours(3);
        if (nowTime > startTime)
            startTime = startTime.AddDays(1);
        TimeSpan waitTime = startTime - nowTime;
        _timer = new Timer(DoWork, null, waitTime,
            TimeSpan.FromHours(24));
        return Task.CompletedTask;
    }

    private async void DoWork(object? state)
    {
        using var scope = serviceProvider.CreateScope();
        var statsService = scope.ServiceProvider.GetRequiredService<IStatsService>();
        await statsService.SeedPlayerInfos();
        var mmrServie = scope.ServiceProvider.GetRequiredService<FireMmrService>();
        await mmrServie.CalcMmmr();
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

