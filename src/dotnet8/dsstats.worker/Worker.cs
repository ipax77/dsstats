using System.Security.Principal;

namespace dsstats.worker;

public sealed class WindowsBackgroundService : BackgroundService
{
    private readonly DsstatsService dsstatsService;
    private readonly ILogger<WindowsBackgroundService> logger;

    public WindowsBackgroundService(
        DsstatsService dsstatsService,
        ILogger<WindowsBackgroundService> logger) =>
        (this.dsstatsService, this.logger) = (dsstatsService, logger);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            // await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            var random = new Random();

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await dsstatsService.StartJob(stoppingToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "{Message}", ex.Message);
                }
                var delayMinutes = random.Next(40, 81);
                await Task.Delay(TimeSpan.FromMinutes(delayMinutes), stoppingToken);
                // await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
        catch (TaskCanceledException)
        {
            // When the stopping token is canceled, for example, a call made from services.msc,
            // we shouldn't exit with a non-zero exit code. In other words, this is expected...
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{Message}", ex.Message);

            // Terminates this process and returns an exit code to the operating system.
            // This is required to avoid the 'BackgroundServiceExceptionBehavior', which
            // performs one of two scenarios:
            // 1. When set to "Ignore": will do nothing at all, errors cause zombie services.
            // 2. When set to "StopHost": will cleanly stop the host, and log errors.
            //
            // In order for the Windows Service Management system to leverage configured
            // recovery options, we need to terminate the process with a non-zero exit code.
            Environment.Exit(1);
        }
    }
}