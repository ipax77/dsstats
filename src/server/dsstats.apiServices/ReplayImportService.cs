using dsstats.shared;
using dsstats.shared.Interfaces;
using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace dsstats.apiServices;

public sealed class ReplayImportService(IHttpClientFactory httpClientFactory) : IReplayImportService
{
    private readonly HttpClient httpClient = httpClientFactory.CreateClient("api");

    public async Task<ReplayImportResultDto?> ImportReplayWithSpawnPlayback(
        ReplayDto replay,
        SpawnPlaybackEncodedSidecar spawnPlayback,
        CancellationToken token = default)
    {
        try
        {
            using var content = new MultipartFormDataContent();
            content.Add(
                new StringContent(JsonSerializer.Serialize(replay), Encoding.UTF8, "application/json"),
                "replay");

            var sidecarContent = new ByteArrayContent(spawnPlayback.Payload);
            sidecarContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            content.Add(sidecarContent, "sidecar", "spawn-playback.bin");

            content.Add(CreateInvariantStringContent(spawnPlayback.FormatVersion), "formatVersion");
            content.Add(CreateInvariantStringContent((byte)spawnPlayback.Compression), "compression");
            content.Add(CreateInvariantStringContent(spawnPlayback.CompressedLength), "compressedLength");
            content.Add(CreateInvariantStringContent(spawnPlayback.UncompressedLength), "uncompressedLength");
            content.Add(CreateInvariantStringContent(spawnPlayback.UnitCount), "unitCount");

            using var request = new HttpRequestMessage(HttpMethod.Post, "api10/upload/import-spawn-playback")
            {
                Content = content
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("DS8upload77");

            using var response = await httpClient.SendAsync(request, token);
            if (!response.IsSuccessStatusCode)
            {
                return new()
                {
                    Error = await response.Content.ReadAsStringAsync(token)
                };
            }

            return await response.Content.ReadFromJsonAsync<ReplayImportResultDto>(cancellationToken: token);
        }
        catch (Exception ex)
        {
            return new() { Error = ex.Message };
        }
    }

    private static StringContent CreateInvariantStringContent<T>(T value)
        where T : IFormattable
    {
        return new StringContent(value.ToString(null, CultureInfo.InvariantCulture) ?? string.Empty);
    }
}
