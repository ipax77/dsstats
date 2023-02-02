using pax.dsstats.dbng.Repositories;
using pax.dsstats.dbng.Services;
using System.Diagnostics;

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
            var importService = scope.ServiceProvider.GetRequiredService<ImportService>();

            Stopwatch sw = Stopwatch.StartNew();

            var result = await importService.ImportReplayBlobs();

            if (result.SavedReplays > 0)
            {
                var statsService = scope.ServiceProvider.GetRequiredService<IStatsService>();
                statsService.ResetStatsCache();
                await statsService.GetRequestStats(new shared.StatsRequest() { Uploaders = false });

                var mmrProduceService = scope.ServiceProvider.GetRequiredService<MmrProduceService>();

                if (result.ContinueReplays.Any())
                {
                    await mmrProduceService.ProduceRatings(new(false), result.LatestReplay, result.ContinueReplays);
                }
                else
                {
                    await mmrProduceService.ProduceRatings(new(true));
                }
                logger.LogWarning($"Replays saved: {result.SavedReplays} ({result.ContinueReplays.Count}) - {result.LatestReplay}");
            }

            var replayRepository = scope.ServiceProvider.GetRequiredService<IReplayRepository>();
            await replayRepository.SetReplayViews();
            await replayRepository.SetReplayDownloads();

            var tourneyService = scope.ServiceProvider.GetRequiredService<TourneyService>();
            await tourneyService.CollectTourneyReplays();

            sw.Stop();
            logger.LogWarning($"{DateTime.UtcNow.ToString(@"yyyy-MM-dd HH:mm:ss")} - Work done in {sw.ElapsedMilliseconds} ms");
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

