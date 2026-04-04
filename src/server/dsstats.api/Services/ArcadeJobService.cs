using dsstats.dbServices;
using dsstats.shared.Interfaces;
using sc2arcade.crawler;

namespace dsstats.api.Services;

public class ArcadeJobService(IServiceScopeFactory scopeFactory, ILogger<ArcadeJobService> logger)
{
    private readonly SemaphoreSlim _lock = new(1, 1);

    public bool IsRunning => _lock.CurrentCount == 0;

    /// <summary>
    /// Runs the full 3 AM arcade import pipeline.
    /// Returns false immediately if the job is already running.
    /// </summary>
    public async Task<bool> RunAsync(CancellationToken token = default)
    {
        if (!await _lock.WaitAsync(0, token))
        {
            logger.LogWarning("Arcade job is already running – ignoring trigger.");
            return false;
        }

        try
        {
            logger.LogInformation("Arcade job started.");
            using var scope = scopeFactory.CreateAsyncScope();

            var crawlerService = scope.ServiceProvider.GetRequiredService<ICrawlerService>();
            await crawlerService.GetLobbyHistory(DateTime.Today.AddDays(-5), token);

            var importService = scope.ServiceProvider.GetRequiredService<IImportService>();
            await importService.CheckDuplicateCandidates();
            await importService.CheckRealmDuplicateCandidates();
            await importService.FixPlayerNames();

            var ratingService = scope.ServiceProvider.GetRequiredService<IRatingService>();
            await ratingService.CreateRatings();

            logger.LogInformation("Arcade job completed.");
            return true;
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            logger.LogInformation("Arcade job was cancelled.");
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Arcade job failed.");
            return false;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<bool> RunCalcAsync(CancellationToken token = default)
    {
        if (!await _lock.WaitAsync(0, token))
        {
            logger.LogWarning("Arcade job is already running – ignoring trigger.");
            return false;
        }

        try
        {
            logger.LogInformation("Arcade job started.");
            using var scope = scopeFactory.CreateAsyncScope();

            var importService = scope.ServiceProvider.GetRequiredService<IImportService>();
            await importService.CheckDuplicateCandidates();
            await importService.CheckRealmDuplicateCandidates();
            await importService.FixPlayerNames();

            var ratingService = scope.ServiceProvider.GetRequiredService<IRatingService>();
            await ratingService.CreateRatings();

            logger.LogInformation("Arcade job completed.");
            return true;
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            logger.LogInformation("Arcade job was cancelled.");
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Arcade job failed.");
            return false;
        }
        finally
        {
            _lock.Release();
        }
    }
}
