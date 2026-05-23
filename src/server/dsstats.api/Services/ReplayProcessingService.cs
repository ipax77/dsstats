using dsstats.api.Hubs;
using dsstats.db;
using dsstats.dbServices;
using dsstats.parser;
using dsstats.shared;
using dsstats.shared.Upload;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using s2protocol.NET;
using System.Threading.Channels;

namespace dsstats.api.Services;

public class ReplayProcessingService(
    ILogger<ReplayProcessingService> logger,
    IDbContextFactory<DsstatsContext> contextFactory,
    Channel<ReplayUploadJob> uploadChannel,
    IImportService importService,
    IHubContext<UploadHub> uploadHub
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

        await using var context = await contextFactory.CreateDbContextAsync(token);

        try
        {
            // 1. Process blob
            var sc2Replay = await replayDecoder.DecodeAsync(job.BlobFilePath, replayDecoderOptions, token);
            ArgumentNullException.ThrowIfNull(sc2Replay, "decoding replay failed.");

            var directStrikeReplay = DsstatsParser.ParseDirectStrikeReplay(sc2Replay);
            var spawnPlaybackSidecar = SpawnPlaybackSidecarFactory.Create(sc2Replay, directStrikeReplay);
            var replay = DsstatsParser.ParseReplay(sc2Replay);
            ArgumentNullException.ThrowIfNull(replay, "parsing replay failed.");

            // 2. Insert into DB
            await importService.InsertReplays([replay]);
            await SaveSpawnPlayback(replay.ComputeHash(), spawnPlaybackSidecar, token);

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

    private async Task SaveSpawnPlayback(string replayHash, SpawnPlaybackSidecarDto sidecar, CancellationToken token)
    {
        var replayId = await contextFactory.CreateDbContextAsync(token);
        await using var context = replayId;
        int dbReplayId = await context.Replays
            .Where(x => x.ReplayHash == replayHash)
            .Select(x => x.ReplayId)
            .FirstOrDefaultAsync(token);
        if (dbReplayId == 0)
        {
            logger.LogWarning("Replay {ReplayHash} was imported but not found for spawn playback sidecar save", replayHash);
            return;
        }

        var encoded = SpawnPlaybackSidecarCodec.EncodeWithMetadata(sidecar);

        var existing = await context.ReplaySpawnPlaybacks.FindAsync([dbReplayId], token);
        if (existing is null)
        {
            context.ReplaySpawnPlaybacks.Add(new()
            {
                ReplayId = dbReplayId,
                FormatVersion = SpawnPlaybackSidecarCodec.FormatVersion,
                Compression = SpawnPlaybackSidecarCodec.Compression,
                CompressedLength = encoded.CompressedLength,
                UncompressedLength = encoded.UncompressedLength,
                UnitCount = encoded.UnitCount,
                Payload = encoded.Payload,
                CreatedAt = DateTime.UtcNow,
            });
        }
        else
        {
            existing.FormatVersion = SpawnPlaybackSidecarCodec.FormatVersion;
            existing.Compression = SpawnPlaybackSidecarCodec.Compression;
            existing.CompressedLength = encoded.CompressedLength;
            existing.UncompressedLength = encoded.UncompressedLength;
            existing.UnitCount = encoded.UnitCount;
            existing.Payload = encoded.Payload;
            existing.CreatedAt = DateTime.UtcNow;
        }

        await context.SaveChangesAsync(token);
    }

    private async Task NotifyHub(DecodeFinishedEventArgs e)
    {
        await uploadHub
        .Clients
        .Group(e.Guid.ToString())
        .SendAsync("DecodeResult", e);
    }
}
