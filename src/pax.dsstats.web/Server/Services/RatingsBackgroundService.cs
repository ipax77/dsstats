using pax.dsstats.dbng.Repositories;
using pax.dsstats.dbng.Services;
using pax.dsstats.dbng.Services.Ratings;
using pax.dsstats.web.Server.Services.Arcade;
using System.Diagnostics;

namespace pax.dsstats.web.Server.Services;



public class RatingsBackgroundService : IHostedService, IDisposable
{
    private readonly IServiceProvider serviceProvider;
    private readonly ILogger<RatingsBackgroundService> logger;
    private Timer? _timer;

    public RatingsBackgroundService(IServiceProvider serviceProvider, ILogger<RatingsBackgroundService> logger)
    {
        this.serviceProvider = serviceProvider;
        this.logger = logger;
    }

    public Task StartAsync(CancellationToken stoppingToken)
    {
        // run every day a 3 am utc
        DateTime nowTime = DateTime.UtcNow;
        DateTime startTime = DateTime.UtcNow.Date.AddHours(3);
        if (nowTime > startTime)
            startTime = startTime.AddDays(1);
        TimeSpan waitTime = startTime - nowTime;
        _timer = new Timer(DoWork, null, waitTime,
            TimeSpan.FromHours(24));
        return Task.CompletedTask;
    }

    private async void DoWork(object? state)
    {
        Stopwatch sw = Stopwatch.StartNew();

        using var scope = serviceProvider.CreateScope();
        var cheatDetectService = scope.ServiceProvider.GetRequiredService<CheatDetectService>();
        await cheatDetectService.DetectNoUpload();

        var replayRepository = scope.ServiceProvider.GetRequiredService<IReplayRepository>();
        await replayRepository.FixPlayerNames();

        var crawlerService = scope.ServiceProvider.GetRequiredService<CrawlerService>();
        await crawlerService.GetLobbyHistory(DateTime.Today.AddDays(-6));

        var arcadeRatingsService = scope.ServiceProvider.GetRequiredService<ArcadeRatingsService>();
        // await arcadeRatingsService.ProduceRatings(DateTime.Today.Day == 1);
        await arcadeRatingsService.ProduceRatings(recalc: true);

        await replayRepository.FixArcadePlayerNames();

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

