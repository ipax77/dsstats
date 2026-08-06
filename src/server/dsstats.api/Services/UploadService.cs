using dsstats.db;
using dsstats.dbServices;
using dsstats.shared.Upload;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Threading.Channels;

namespace dsstats.api.Services;

public partial class UploadService(
    IDbContextFactory<DsstatsContext> contextFactory,
    Channel<UploadJob> uploadChannel,
    Channel<ReplayUploadJob> replaysChannel,
    IImportService importService,
    IOptions<UploadStorageOptions> uploadStorageOptions,
    ILogger<UploadService> logger)
{
    private static readonly Version ApiVersion =
        ReplayDecoderVersion.GetReleaseVersion(typeof(UploadService).Assembly);
    private static readonly string ApiUploadVersion =
        ReplayDecoderVersion.Format(ReplayDecoderSource.Api, ApiVersion);
    private readonly UploadStorageOptions storageOptions = uploadStorageOptions.Value;

    public async Task<bool> ProcessUploadAsync(UploadDto uploadDto)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        try
        {
            var filePath = await StoreBlob(uploadDto);
            List<int> playerIds = [];
            foreach (var requestName in uploadDto.RequestNames)
            {
                var playerId = importService.GetOrCreatePlayerId(requestName.Name, requestName.RegionId, requestName.RealmId, requestName.ToonId);
                playerIds.Add(playerId);
            }

            var uploadJob = new UploadJob
            {
                PlayerIds = playerIds.ToArray(),
                Version = uploadDto.AppVersion,
                BlobFilePath = filePath,
                CreatedAt = DateTime.UtcNow,
            };
            SetDecoderVersion(uploadJob, uploadDto.AppVersion);
            context.UploadJobs.Add(uploadJob);
            await context.SaveChangesAsync();

            await uploadChannel.Writer.WriteAsync(uploadJob);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing upload");
            return false;
        }
    }

    public async Task<bool> ProcessUploadAsync(UploadRequestDto request)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        try
        {
            var filePath = await StoreBlob(request);
            List<int> playerIds = [];
            foreach (var requestName in request.RequestNames)
            {
                var playerId = importService.GetOrCreatePlayerId(requestName.Name, requestName.RegionId, requestName.RealmId, requestName.ToonId);
                playerIds.Add(playerId);
            }

            var uploadJob = new UploadJob
            {
                PlayerIds = playerIds.ToArray(),
                Version = request.AppVersion,
                BlobFilePath = filePath,
                CreatedAt = DateTime.UtcNow,
            };
            SetDecoderVersion(uploadJob, request.AppVersion);
            context.UploadJobs.Add(uploadJob);
            await context.SaveChangesAsync();

            await uploadChannel.Writer.WriteAsync(uploadJob);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing upload");
            return false;
        }
    }

    public async Task<DecodeRequestResult> SaveReplay(Guid guid, IFormFile file)
    {
        if (file.Length == 0)
        {
            return new()
            {
                Error = "Invalid file size."
            };
        }

        await using var context = await contextFactory.CreateDbContextAsync();

        var queueCount = 1;

        try
        {
            var filePath = await StoreReplay(guid, file);

            var uploadJob = new ReplayUploadJob
            {
                Guid = guid,
                Version = ApiUploadVersion,
                DecoderSource = ReplayDecoderSource.Api,
                DecoderVersion = ApiVersion.ToString(3),
                BlobFilePath = filePath,
                CreatedAt = DateTime.UtcNow,
            };
            context.ReplayUploadJobs.Add(uploadJob);
            await context.SaveChangesAsync();

            await replaysChannel.Writer.WriteAsync(uploadJob);
            return new() { Success = true, QueuePosition = queueCount };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing replay upload");
            return new() { Error = "Unknown Error", QueuePosition = queueCount };
        }
    }

    private static void SetDecoderVersion(UploadJob uploadJob, string? rawVersion)
    {
        var parsed = ReplayDecoderVersion.Parse(rawVersion);
        uploadJob.DecoderSource = parsed.Source;
        uploadJob.DecoderVersion = ReplayDecoderVersion.LimitVersionForStorage(parsed.Version);
    }
}

