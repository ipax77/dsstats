using dsstats.indexedDb.Services;
using dsstats.shared.Upload;
using System.Net.Http.Headers;

namespace dsstats.pwa.Services;

public partial class DecodeService
{
    public async Task Upload10(IndexedDbService dbService)
    {
        var config = await pwaConfigService.GetConfig();
        try
        {
            var requestNames = await GetUploadRequestNames(dbService);
            UploadRequestDto uploadDto = new()
            {
                AppGuid = config.AppGuid,
                AppVersion = "myds" + Version.ToString(),
                RequestNames = requestNames,
            };

            var httpClient = httpClientFactory.CreateClient("api");

            logger.LogInformation("Starting upload of replays...");
            var exportResult = await dbService.GetExportReplays10(uploadDto, 50);
            logger.LogInformation("Found {Count} replays to upload.", exportResult.Hashes.Count);

            if (exportResult.Hashes.Count > 0)
            {
                OnDecodeStateChanged(new DecodeInfoEventArgs
                {
                    UploadStatus = UploadStatus.Uploading
                });
            }

            while (exportResult.Hashes.Count > 0)
            {
                UploadRequestDto uploadStepDto = new()
                {
                    AppGuid = config.AppGuid,
                    AppVersion = $"myds{Version}",
                    RequestNames = requestNames,
                };
                var content = new ByteArrayContent(exportResult.Payload);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                content.Headers.ContentEncoding.Add("gzip");
                content.Headers.ContentLength = exportResult.Payload.Length;

                var response = await httpClient.PostAsync("api10/Upload", content);
                response.EnsureSuccessStatusCode();
                logger.LogInformation("Upload Successful");
                await dbService.MarkReplaysAsUploaded(exportResult.Hashes);
                exportResult = await dbService.GetExportReplays10(uploadStepDto, 250);
            }
        }
        catch (Exception ex)
        {
            logger.LogError("Failed to upload replays: {Message}", ex.Message);
            OnDecodeStateChanged(new DecodeInfoEventArgs
            {
                UploadStatus = UploadStatus.UploadError,
                Info = $"Failed to upload replays: {ex.Message}"
            });
            return;
        }
        OnDecodeStateChanged(new DecodeInfoEventArgs
        {
            UploadStatus = UploadStatus.UploadSuccess
        });
    }


    private static async Task<List<RequestNames>> GetUploadRequestNames(IndexedDbService dbService)
    {
        var trackedProfiles = await dbService.GetTrackedProfiles();
        return trackedProfiles
            .Where(profile => profile.ToonId.Id > 0)
            .Select(profile => new RequestNames
            {
                Name = profile.Name,
                ToonId = profile.ToonId.Id,
                RegionId = profile.ToonId.Region,
                RealmId = profile.ToonId.Realm,
            })
            .ToList();
    }
}
