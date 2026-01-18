using dsstats.service.Models;
using dsstats.service.Services;
using Microsoft.Extensions.Options;

namespace dsstats.service;

internal sealed class Worker(DsstatsService dsstatsService, IOptions<DsstatsConfig> dsstatsConfig, ILogger<Worker> logger) : BackgroundService
{
    private int jobCounter;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromMinutes(dsstatsConfig.Value.StartDelayInMinutes), stoppingToken);
            await dsstatsService.Update(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                jobCounter++;
                if (jobCounter % 10 == 0)
                {
                    await dsstatsService.Update(stoppingToken);
                }
                try
                {
                    await dsstatsService.StartImportAsync(stoppingToken);
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    logger.LogError("Decode process failed: {error}", ex.Message);
                }
#pragma warning disable CA5394 // Do not use insecure randomness
                var delayMinutes = Random.Shared.Next(40, 81);
#pragma warning restore CA5394 // Do not use insecure randomness
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
