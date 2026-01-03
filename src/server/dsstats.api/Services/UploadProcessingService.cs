using dsstats.db;
using dsstats.db.Old;
using dsstats.dbServices;
using dsstats.shared;
using Microsoft.EntityFrameworkCore;
using System.IO.Compression;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;

namespace dsstats.api.Services;

public class UploadProcessingService(
    ILogger<UploadProcessingService> logger,
    IServiceScopeFactory scopeFactory
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("UploadProcessingService started");

        var uploadChannel =
            scopeFactory.CreateScope()
                        .ServiceProvider
                        .GetRequiredService<Channel<UploadJob>>();

        await EnqueueUnfinishedJobs(uploadChannel, stoppingToken);

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
                logger.LogError(ex, "UploadProcessingService crashed, restarting...");
                await Task.Delay(1000, stoppingToken);
            }
        }

        logger.LogInformation("UploadProcessingService stopped");
    }

    private async Task ProcessJob(UploadJob job, CancellationToken token)
    {
        logger.LogInformation("Processing upload job {JobId}", job.UploadJobId);

        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();
        var importService = scope.ServiceProvider.GetRequiredService<IImportService>();

        try
        {
            // 1. Load blob
            var replays = await LoadReplays(job.BlobFilePath);

            // 2. Insert into DB
            await importService.InsertReplays(replays);

            // 3. Update job metadata
            var dbJob = await context.UploadJobs.FindAsync(job.UploadJobId);
            if (dbJob != null)
            {
                dbJob.FinishedAt = DateTime.UtcNow;
                dbJob.Error = null;
                await context.SaveChangesAsync(token);
            }

            logger.LogInformation("Job {JobId} completed successfully", job.UploadJobId);
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown — do NOT mark job as error
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing job {JobId}", job.UploadJobId);

            try
            {
                var dbJob = await context.UploadJobs.FindAsync(job.UploadJobId);
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
                    "Failed to update job {JobId} with error information", job.UploadJobId);
            }
        }
    }

    private async Task EnqueueUnfinishedJobs(Channel<UploadJob> uploadChannel, CancellationToken token)
    {
        logger.LogInformation("Checking for unfinished upload jobs...");

        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();

        //  Load jobs where FinishedAt IS NULL
        var unfinishedJobs = await context.UploadJobs
            .Where(j => j.FinishedAt == null)
            .OrderBy(j => j.CreatedAt)
            .ToListAsync(token);

        if (unfinishedJobs.Count == 0)
        {
            logger.LogInformation("No unfinished jobs.");
            return;
        }

        logger.LogInformation("Requeueing {Count} unfinished jobs", unfinishedJobs.Count);

        foreach (var job in unfinishedJobs)
        {
            await uploadChannel.Writer.WriteAsync(job, token);
        }
    }

    private static async Task<List<ReplayDto>> LoadReplays(string filePath)
    {
        if (filePath.EndsWith("blob", StringComparison.OrdinalIgnoreCase))
        {
            using var fs = File.OpenRead(filePath);
            using var gz = new GZipStream(fs, CompressionMode.Decompress);
            using var ms = new MemoryStream();
            await gz.CopyToAsync(ms);
            var json = Encoding.UTF8.GetString(ms.ToArray());
            var replays = JsonSerializer.Deserialize<List<ReplayV2Dto>>(json) ?? [];
            return replays.Select(s => s.ToV3Dto()).ToList();
        }
        else if (filePath.EndsWith("json.gz", StringComparison.OrdinalIgnoreCase))
        {
            await using var fs = File.OpenRead(filePath);
            await using var gz = new GZipStream(fs, CompressionMode.Decompress);

            return await JsonSerializer.DeserializeAsync<List<ReplayDto>>(gz)
                   ?? [];
        }
        return [];
    }
}
