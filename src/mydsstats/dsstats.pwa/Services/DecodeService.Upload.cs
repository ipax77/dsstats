using dsstats.indexedDb.Services;
using dsstats.shared.Upload;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

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
                using var response = exportResult.Sidecars.Count > 0
                    ? await PostSpawnPlaybackBatch(httpClient, exportResult)
                    : await PostReplayBatch(httpClient, exportResult);
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

    private static async Task<HttpResponseMessage> PostReplayBatch(HttpClient httpClient, ExportResult exportResult)
    {
        using var content = new ByteArrayContent(exportResult.Payload);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        content.Headers.ContentEncoding.Add("gzip");
        content.Headers.ContentLength = exportResult.Payload.Length;

        return await httpClient.PostAsync("api10/Upload", content);
    }

    private static async Task<HttpResponseMessage> PostSpawnPlaybackBatch(HttpClient httpClient, ExportResult exportResult)
    {
        using var multipart = new MultipartFormDataContent();

        var requestContent = new ByteArrayContent(exportResult.Payload);
        requestContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        requestContent.Headers.ContentEncoding.Add("gzip");
        multipart.Add(requestContent, "request", "request.json.gz");

        var manifest = exportResult.Sidecars
            .Select(sidecar => new SpawnPlaybackUploadManifestEntryDto
            {
                ReplayHash = sidecar.ReplayHash,
                PartName = sidecar.PartName,
                FormatVersion = sidecar.FormatVersion,
                Compression = sidecar.Compression,
                CompressedLength = sidecar.CompressedLength,
                UncompressedLength = sidecar.UncompressedLength,
                UnitCount = sidecar.UnitCount,
            })
            .ToList();

        multipart.Add(
            new StringContent(JsonSerializer.Serialize(manifest, JsonSerializerOptions.Web), Encoding.UTF8, "application/json"),
            "manifest");

        foreach (var sidecar in exportResult.Sidecars)
        {
            var sidecarContent = new ByteArrayContent(sidecar.Payload);
            sidecarContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            multipart.Add(sidecarContent, sidecar.PartName, $"{sidecar.PartName}.bin");
        }

        return await httpClient.PostAsync("api10/upload/import-spawn-playbacks", multipart);
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
