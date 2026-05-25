using dsstats.api.Hubs;
using dsstats.dbServices;
using dsstats.dbServices.BuildDetails;
using dsstats.dbServices.InHouse;
using dsstats.shared.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace dsstats.api.Services;

public class TimedHostedService(
    ArcadeJobService arcadeJobService,
    IRatingService ratingService,
    BuildDetailGenerationService buildDetailGenerationService,
    IInHouseGameSessionService sessionService,
    ReplayUserRatingService replayUserRatingService,
    IHubContext<InHouseHub> hubContext,
    ILogger<TimedHostedService> logger) : BackgroundService
{
    private static readonly TimeSpan InHouseInactivityLimit = TimeSpan.FromHours(12);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var timedWork = RunTimedWorkAsync(stoppingToken);

        await Task.WhenAll(timedWork);
    }

    private async Task RunTimedWorkAsync(CancellationToken stoppingToken)
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

            if (nowTime.Hour == 3)
            {
                await arcadeJobService.RunAsync(token);
                var result = await buildDetailGenerationService.ProcessPendingBatchAsync(token: token);
                logger.LogInformation(
                    "Generated replay build details: {Detected} detected, {NotDetectable} not detectable, {Failed} failed from {Candidates} candidates.",
                    result.Detected,
                    result.NotDetectable,
                    result.Failed,
                    result.Candidates);
                await CloseInactiveInHouseSessionsAsync(token);
            }
            else
            {
                await ratingService.ContinueRatings();
                await CollectReplayUserRatingsAsync(token);
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            // Graceful shutdown
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while running timed hosted service work.");
        }
    }

    private async Task CloseInactiveInHouseSessionsAsync(CancellationToken token)
    {
        var closed = await sessionService.CloseInactiveSessionsAsync(InHouseInactivityLimit, token);
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

    private async Task CollectReplayUserRatingsAsync(CancellationToken token)
    {
        try
        {
            var collected = await replayUserRatingService.CollectPendingVotesAsync(token);
            if (collected > 0)
            {
                logger.LogWarning("Collected {Count} replay user ratings.", collected);
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while collecting replay user ratings.");
        }
    }
}
