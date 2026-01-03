
using dsstats.shared.Interfaces;

namespace dsstats.api.Services;

public class TimedHostedService(IServiceScopeFactory scopeFactory, ILogger<TimedHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        DateTime nowTime = DateTime.UtcNow;
        DateTime startTime = new(nowTime.Year, nowTime.Month, nowTime.Day, nowTime.Hour, 30, 0);
        if (nowTime.Minute >= 30)
            startTime = startTime.AddHours(1);
        TimeSpan waitTime = startTime - nowTime;
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

    private async Task DoWork(CancellationToken token)
    {
        try
        {
            DateTime nowTime = DateTime.UtcNow;

            using var scope = scopeFactory.CreateAsyncScope();
            var ratingService = scope.ServiceProvider.GetRequiredService<IRatingService>();

            if (nowTime.Hour == 3)
            {
                await ratingService.CreateRatings();
            }
            else
            {
                await ratingService.ContinueRatings();
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            // Graceful shutdown
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while importing replays.");
        }
    }
}
