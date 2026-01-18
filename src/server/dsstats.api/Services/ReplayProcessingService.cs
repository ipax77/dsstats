using dsstats.api.Hubs;
using dsstats.db;
using dsstats.dbServices;
using dsstats.parser;
using dsstats.shared;
using dsstats.shared.Upload;
using Microsoft.AspNetCore.SignalR;
using s2protocol.NET;
using System.Threading.Channels;

namespace dsstats.api.Services;

public class ReplayProcessingService(
    ILogger<ReplayProcessingService> logger,
    IServiceScopeFactory scopeFactory
) : BackgroundService
{
    private readonly ReplayDecoder replayDecoder = new();
    private readonly ReplayDecoderOptions replayDecoderOptions = new()
    {
        Initdata = true,
        Details = true,
        Metadata = true,
        GameEvents = false,
        MessageEvents = false,
        TrackerEvents = true,
        AttributeEvents = false,
    };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("ReplayProcessingService started");

        var uploadChannel =
            scopeFactory.CreateScope()
                        .ServiceProvider
                        .GetRequiredService<Channel<ReplayUploadJob>>();


        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await foreach (var job in uploadChannel.Reader.ReadAllAsync(stoppingToken))
                {
                    await ProcessJob(job, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "ReplayProcessingService crashed, restarting...");
                await Task.Delay(1000, stoppingToken);
            }
        }

        logger.LogInformation("ReplayProcessingService stopped");
    }

    private async Task ProcessJob(ReplayUploadJob job, CancellationToken token)
    {
        logger.LogInformation("Processing upload job {JobId}", job.ReplayUploadJobId);

        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();
        var importService = scope.ServiceProvider.GetRequiredService<IImportService>();

        try
        {
            // 1. Process blob
            var sc2Replay = await replayDecoder.DecodeAsync(job.BlobFilePath, replayDecoderOptions, token);
            ArgumentNullException.ThrowIfNull(sc2Replay, "decoding replay failed.");

            var replay = DsstatsParser.ParseReplay(sc2Replay);
            ArgumentNullException.ThrowIfNull(replay, "parsing replay failed.");

            // 2. Insert into DB
            await importService.InsertReplays([replay]);

            // 3. Update job metadata
            var dbJob = await context.ReplayUploadJobs.FindAsync(job.ReplayUploadJobId, token);
            if (dbJob != null)
            {
                dbJob.FinishedAt = DateTime.UtcNow;
                dbJob.Error = null;
                await context.SaveChangesAsync(token);
            }
            await NotifyHub(new()
            {
                Guid = job.Guid,
                ReplayHash = replay.ComputeHash()
            });

            logger.LogInformation("Job {JobId} completed successfully", job.ReplayUploadJobId);
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown — do NOT mark job as error
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing job {JobId}", job.ReplayUploadJobId);

            try
            {
                var dbJob = await context.ReplayUploadJobs.FindAsync(job.ReplayUploadJobId);
                if (dbJob != null)
                {
                    // Ensure error length < 200
                    string msg = ex.Message;
                    if (msg.Length > 200)
                        msg = msg[..200];

                    dbJob.Error = msg;
                    dbJob.FinishedAt = DateTime.UtcNow;

                    await context.SaveChangesAsync(token);
                }
            }
            catch (Exception updateEx)
            {
                logger.LogError(updateEx,
                    "Failed to update job {JobId} with error information", job.ReplayUploadJobId);
            }
            await NotifyHub(new()
            {
                Guid = job.Guid,
                ReplayHash = string.Empty,
                Error = ex.Message,
            });
        }
    }

    private async Task NotifyHub(DecodeFinishedEventArgs e)
    {
        using var scope = scopeFactory.CreateScope();
        var hub = scope.ServiceProvider.GetRequiredService<IHubContext<UploadHub>>();
        await hub
        .Clients
        .Group(e.Guid.ToString())
        .SendAsync("DecodeResult", e);
    }
}
