using dsstats.indexedDb.Services;
using dsstats.shared.Upload;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace dsstats.pwa.Services;

public partial class DecodeService
{
    public async Task Upload10(IndexedDbService dbService)
    {
        var config = await pwaConfigService.GetConfig();
        try
        {
            UploadRequestDto uploadDto = new()
            {
                AppGuid = config.AppGuid,
                AppVersion = config.ConfigVersion,
                RequestNames = [],
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
                    AppVersion = config.ConfigVersion,
                    RequestNames = [],
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
}

public record UploadDto
{
    public Guid AppGuid { get; init; }
    public string AppVersion { get; init; } = string.Empty;
    public List<RequestNames> RequestNames { get; init; } = new();
    public string Base64ReplayBlob { get; init; } = "";
}

public record RequestNames
{
    public RequestNames(string name, int toonId, int regionId, int realmId)
    {
        Name = name;
        ToonId = toonId;
        RegionId = regionId;
        RealmId = realmId;
    }

    public RequestNames() { }

    public string Name { get; set; } = "";
    public int ToonId { get; init; }
    public int RegionId { get; set; }
    public int RealmId { get; set; } = 1;
}