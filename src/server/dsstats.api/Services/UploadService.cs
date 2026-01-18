using dsstats.db;
using dsstats.dbServices;
using dsstats.shared.Upload;
using System.Threading.Channels;

namespace dsstats.api.Services;

public partial class UploadService(IServiceScopeFactory serviceScopeFactory, ILogger<UploadService> logger)
{
    public async Task<bool> ProcessUploadAsync(UploadDto uploadDto)
    {
        using var scope = serviceScopeFactory.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();
        var uploadChannel = scope.ServiceProvider.GetRequiredService<Channel<UploadJob>>();
        var importService = scope.ServiceProvider.GetRequiredService<IImportService>();

        try
        {
            var filePath = await StoreBlob(uploadDto);
            List<int> playerIds = [];
            foreach (var requestName in uploadDto.RequestNames)
            {
                var playerId = importService.GetOrCreatePlayerId(requestName.Name, requestName.RegionId, requestName.RealmId, requestName.ToonId, context);
                playerIds.Add(playerId);
            }

            var uploadJob = new UploadJob
            {
                PlayerIds = playerIds.ToArray(),
                Version = uploadDto.AppVersion,
                BlobFilePath = filePath,
                CreatedAt = DateTime.UtcNow,
            };
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
        using var scope = serviceScopeFactory.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();
        var uploadChannel = scope.ServiceProvider.GetRequiredService<Channel<UploadJob>>();
        var importService = scope.ServiceProvider.GetRequiredService<IImportService>();

        try
        {
            var filePath = await StoreBlob(request);
            List<int> playerIds = [];
            foreach (var requestName in request.RequestNames)
            {
                var playerId = importService.GetOrCreatePlayerId(requestName.Name, requestName.RegionId, requestName.RealmId, requestName.ToonId, context);
                playerIds.Add(playerId);
            }

            var uploadJob = new UploadJob
            {
                PlayerIds = playerIds.ToArray(),
                Version = request.AppVersion,
                BlobFilePath = filePath,
                CreatedAt = DateTime.UtcNow,
            };
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

        using var scope = serviceScopeFactory.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();
        var replaysChannel = scope.ServiceProvider.GetRequiredService<Channel<ReplayUploadJob>>();

        var queueCount = 1;

        try
        {
            var filePath = await StoreReplay(guid, file);

            var uploadJob = new ReplayUploadJob
            {
                Guid = guid,
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
}

