using pax.dsstats.dbng.Repositories;
using pax.dsstats.dbng.Services;
using pax.dsstats.dbng.Services.Ratings;

namespace pax.dsstats.web.Server.Services;

public class CacheBackgroundService : IHostedService, IDisposable
{
    private readonly IServiceProvider serviceProvider;
    private readonly ILogger<CacheBackgroundService> logger;
    private Timer? _timer;
    private SemaphoreSlim ss = new(1, 1);

    public CacheBackgroundService(IServiceProvider serviceProvider, ILogger<CacheBackgroundService> logger)
    {
        this.serviceProvider = serviceProvider;
        this.logger = logger;
    }

    public Task StartAsync(CancellationToken stoppingToken)
    {
        _timer = new Timer(DoWork, null, new TimeSpan(0, 4, 0), new TimeSpan(1, 0, 0));
        // _timer = new Timer(DoWork, null, new TimeSpan(0, 0, 4), new TimeSpan(0, 1, 0));
        return Task.CompletedTask;
    }

    private async void DoWork(object? state)
    {
        await ss.WaitAsync();


        try
        {
            using var scope = serviceProvider.CreateScope();
            //var httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
            //var httpClient = httpClientFactory.CreateClient("ratingsClient");

            //await httpClient.GetAsync("/api/v1/ratings");

            var ratingsService = scope.ServiceProvider.GetRequiredService<RatingsService>();
            await ratingsService.ProduceRatings();

            var statsService = scope.ServiceProvider.GetRequiredService<IStatsService>();
            statsService.ResetStatsCache();

            await statsService.GetRequestStats(new shared.StatsRequest() { Uploaders = false });

            var replayRepository = scope.ServiceProvider.GetRequiredService<IReplayRepository>();
            await replayRepository.SetReplayViews();
            await replayRepository.SetReplayDownloads();

            //var tourneyService = scope.ServiceProvider.GetRequiredService<TourneyService>();
            //await tourneyService.CollectTourneyReplays();
        }
        catch (Exception ex)
        {
            logger.LogError($"job failed: {ex.Message}");
        }
        finally
        {
            ss.Release();
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

internal record CacheBackgroundStatus
{
    public bool ImportDone { get; set; }
    public bool StatsReset { get; set; }
    public bool StatsRebuilt { get; set; }
    public bool RatingsProduced { get; set; }
    public bool ReplayViewsSet { get; set; }
    public bool ReplayDownloadsSet { get; set; }
}