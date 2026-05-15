using dsstats.api.Hubs;
using dsstats.dbServices.InHouse;
using Microsoft.AspNetCore.SignalR;

namespace dsstats.api.InHouse;

public sealed class InHouseSessionCleanupService(
    IInHouseGameSessionService sessionService,
    IHubContext<InHouseHub> hubContext,
    ILogger<InHouseSessionCleanupService> logger) : BackgroundService
{
    private static readonly TimeSpan InactivityLimit = TimeSpan.FromHours(12);
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(10);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new(CheckInterval);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CloseInactiveSessionsAsync(stoppingToken);
                await timer.WaitForNextTickAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to close inactive InHouse sessions.");
            }
        }
    }

    private async Task CloseInactiveSessionsAsync(CancellationToken token)
    {
        var closed = await sessionService.CloseInactiveSessionsAsync(InactivityLimit, token);
        if (closed.Count == 0)
        {
            return;
        }

        foreach (var detail in closed)
        {
            await hubContext.Clients.Group(InHouseHub.GetSessionGroupName(detail.SessionId))
                .SendAsync(InHouseHub.SessionStateEvent, detail, token);
        }

        await hubContext.Clients.All.SendAsync(InHouseHub.ActiveSessionsChangedEvent, token);
    }
}
