
using dsstats.db8services;
using dsstats.shared.Interfaces;
using pax.dsstats.web.Server.Services.Arcade;

namespace dsstats.api.Services;

public class TimedHostedService : BackgroundService
{
    private int executionCount = 0;
    private readonly IServiceScopeFactory scopeFactory;
    private readonly ILogger<TimedHostedService> logger;

    public TimedHostedService(IServiceScopeFactory scopeFactory, ILogger<TimedHostedService> logger)
    {
        this.scopeFactory = scopeFactory;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Timed Hosted Service running.");

        // run every hour at the 30-minute mark
        DateTime nowTime = DateTime.UtcNow;
        DateTime startTime = new DateTime(nowTime.Year, nowTime.Month, nowTime.Day, nowTime.Hour, 30, 0);
        if (nowTime.Minute >= 30)
            startTime = startTime.AddHours(1);
        TimeSpan waitTime = startTime - nowTime;

        logger.LogInformation("Timed Hosted Service first run: {time}", startTime.ToString("yyyy-MM-dd HH:mm:ss"));
        try
        {
            await Task.Delay(waitTime, stoppingToken);
            await DoWork(stoppingToken);

            using PeriodicTimer timer = new(TimeSpan.FromHours(1));


            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await DoWork(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Timed Hosted Service is stopping.");
        }
    }

    // Could also be a async method, that can be awaited in ExecuteAsync above
    private async Task DoWork(CancellationToken token)
    {
        int count = Interlocked.Increment(ref executionCount);

        try
        {
            using var scope = scopeFactory.CreateAsyncScope();
            var ratingService = scope.ServiceProvider.GetRequiredService<IRatingService>();
            var replayRepository = scope.ServiceProvider.GetRequiredService<IReplayRepository>();

            DateTime nowTime = DateTime.UtcNow;
            if (nowTime.Hour == 3)
            {
                await replayRepository.FixDsstatsPlayerNames();
                var crawlerService = scope.ServiceProvider.GetRequiredService<CrawlerService>();
                await crawlerService.GetLobbyHistory(DateTime.Today.AddDays(-6), token);
                await ratingService.ProduceRatings(shared.RatingCalcType.Arcade);
                await replayRepository.FixArcadePlayerNames();
                await ratingService.ProduceRatings(shared.RatingCalcType.Combo);
            }
            else
            {
                await ratingService.ProduceRatings(shared.RatingCalcType.Dsstats);
            }

            await replayRepository.SetReplayViews();
            await replayRepository.SetReplayDownloads();
        }
        catch (Exception ex)
        {
            logger.LogError("background job failed: {error}", ex.Message);
        }
    }
}
