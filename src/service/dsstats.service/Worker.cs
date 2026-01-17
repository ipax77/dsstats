using dsstats.service.Models;
using dsstats.service.Services;
using Microsoft.Extensions.Options;

namespace dsstats.service;

public class Worker(DsstatsService dsstatsService, IOptions<DsstatsConfig> dsstatsConfig, ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromMinutes(dsstatsConfig.Value.StartDelayInMinutes), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await dsstatsService.StartImportAsync(stoppingToken);
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    logger.LogError("Decode process failed: {error}", ex.Message);
                }
                var delayMinutes = Random.Shared.Next(40, 81);
                await Task.Delay(TimeSpan.FromMinutes(delayMinutes), stoppingToken);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            logger.LogError("Dsstats Service worker failed: {error}", ex.Message);
            Environment.Exit(1);
        }
    }
}
