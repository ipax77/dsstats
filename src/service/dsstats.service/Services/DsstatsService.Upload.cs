using dsstats.db;
using dsstats.service.Models;
using dsstats.shared;
using dsstats.shared.Upload;
using Microsoft.EntityFrameworkCore;
using System.IO.Compression;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace dsstats.service.Services;

internal sealed partial class DsstatsService
{
    private const int SpawnPlaybackUploadBatchSize = 50;
    private const int ReplayOnlyUploadBatchSize = 250;
    private static readonly Uri UploadEndpoint = new("api10/Upload", UriKind.Relative);
    private static readonly Uri SpawnPlaybackUploadEndpoint = new("api10/upload/import-spawn-playbacks", UriKind.Relative);
    private static readonly JsonSerializerOptions UploadJsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<int> Upload(AppOptions config, CancellationToken ct)
    {
        if (!config.UploadCredential)
        {
            logger.LogWarning("Upload is disabled by worker config.");
            return 0;
        }

        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();
        var httpClient = httpClientFactory.CreateClient("api");
        httpClient.DefaultRequestHeaders.Authorization = new("DS8upload77");
        var requestNames = GetUploadRequestNames(config);
        var uploadedCount = 0;

        try
        {
            while (!ct.IsCancellationRequested)
            {
                var candidates = await GetUploadCandidates(context, ct);
                if (candidates.Count == 0)
                {
                    break;
                }

                var batchCandidates = SelectUploadBatchCandidates(candidates);
                var replays = await GetUploadReplays(
                    context,
                    batchCandidates.Select(candidate => candidate.ReplayId).ToList(),
                    ct);

                if (replays.Count == 0)
                {
                    break;
                }

                var batch = CreateUploadBatch(config.AppGuid, requestNames, replays);
                using var response = batch.Sidecars.Count > 0
                    ? await PostSpawnPlaybackBatch(httpClient, batch, ct)
                    : await PostReplayBatch(httpClient, batch, ct);
                await EnsureUploadSuccess(response, ct);

                if (batch.Sidecars.Count > 0)
                {
                    var uploadedHashes = await GetUploadedHashes(response, ct);
                    if (uploadedHashes.Count == 0)
                    {
                        throw new InvalidOperationException("Upload succeeded but did not confirm any replay hashes.");
                    }

                    var uploadedReplayIds = GetUploadedReplayIds(batch, uploadedHashes);
                    if (uploadedReplayIds.Count == 0)
                    {
                        throw new InvalidOperationException("Upload succeeded but did not match any local replay hashes.");
                    }

                    await context.Replays
                        .Where(x => uploadedReplayIds.Contains(x.ReplayId))
                        .ExecuteUpdateAsync(e => e.SetProperty(p => p.Uploaded, true), ct);
                    uploadedCount += uploadedReplayIds.Count;
                }
                else
                {
                    await context.Replays
                        .Where(x => batch.ReplayIds.Contains(x.ReplayId))
                        .ExecuteUpdateAsync(e => e.SetProperty(p => p.Uploaded, true), ct);
                    uploadedCount += batch.ReplayIds.Count;
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            logger.LogError("Upload failed: {error}", ex.Message);
        }

        return uploadedCount;
    }

    private static async Task<List<UploadReplayCandidate>> GetUploadCandidates(
        DsstatsContext context,
        CancellationToken ct)
    {
        return await context.Replays
            .Where(x => !x.Uploaded)
            .AsNoTracking()
            .OrderByDescending(o => o.Gametime)
            .Select(x => new UploadReplayCandidate(x.ReplayId, x.SpawnPlayback != null))
            .Take(ReplayOnlyUploadBatchSize)
            .ToListAsync(ct);
    }

    private static List<UploadReplayCandidate> SelectUploadBatchCandidates(List<UploadReplayCandidate> candidates)
    {
        return candidates.Any(candidate => candidate.HasSidecar)
            ? candidates.Take(SpawnPlaybackUploadBatchSize).ToList()
            : candidates;
    }

    private static async Task<List<Replay>> GetUploadReplays(
        DsstatsContext context,
        List<int> replayIds,
        CancellationToken ct)
    {
        return await context.Replays
            .Where(x => replayIds.Contains(x.ReplayId))
            .AsNoTracking()
            .OrderByDescending(o => o.Gametime)
            .Include(i => i.SpawnPlayback)
            .Include(i => i.Players)
                .ThenInclude(i => i.Player)
            .Include(i => i.Players)
                .ThenInclude(i => i.Upgrades)
                    .ThenInclude(i => i.Upgrade)
            .Include(i => i.Players)
                .ThenInclude(i => i.Spawns)
                    .ThenInclude(i => i.Units)
                        .ThenInclude(i => i.Unit)
            .AsSplitQuery()
            .ToListAsync(ct);
    }

    private ServiceUploadBatch CreateUploadBatch(
        Guid appGuid,
        List<RequestNames> requestNames,
        List<Replay> replays)
    {
        List<int> replayIds = new(replays.Count);
        List<UploadReplayHash> replayHashes = new(replays.Count);
        List<ReplayDto> replayDtos = new(replays.Count);
        List<ServiceUploadSidecar> sidecars = [];

        foreach (var replay in replays)
        {
            replayIds.Add(replay.ReplayId);

            var replayDto = replay.ToDto();
            replayDto.FileName = string.Empty;
            var payloadReplayHash = replayDto.ComputeHash();
            replayHashes.Add(new(replay.ReplayId, payloadReplayHash));

            var sidecar = CreateUploadSidecar(replay, payloadReplayHash, sidecars.Count);
            if (sidecar is not null)
            {
                replayDto.SpawnPlayback = new()
                {
                    Available = true,
                    FormatVersion = sidecar.FormatVersion,
                    Compression = sidecar.Compression,
                    CompressedLength = sidecar.CompressedLength,
                    UncompressedLength = sidecar.UncompressedLength,
                    UnitCount = sidecar.UnitCount,
                };
                sidecars.Add(sidecar);
            }

            replayDtos.Add(replayDto);
        }

        return new(
            new UploadRequestDto
            {
                AppGuid = appGuid,
                AppVersion = "ser" + CurrentVersion.ToString(),
                RequestNames = requestNames,
                Replays = replayDtos,
            },
            replayIds,
            replayHashes,
            sidecars);
    }

    private static ServiceUploadSidecar? CreateUploadSidecar(
        Replay replay,
        string payloadReplayHash,
        int sidecarIndex)
    {
        var sidecar = replay.SpawnPlayback;
        if (sidecar is null
            || sidecar.Payload.Length == 0
            || sidecar.CompressedLength != sidecar.Payload.Length
            || sidecar.FormatVersion != SpawnPlaybackSidecarCodec.FormatVersion
            || sidecar.CompressedLength <= 0
            || sidecar.UncompressedLength <= 0
            || sidecar.UnitCount <= 0
            || sidecar.Compression is not (SpawnPlaybackCompression.Brotli or SpawnPlaybackCompression.GZip)
            || !SpawnPlaybackEligibility.IsEligible(replay.Players.Count, replay.Duration))
        {
            return null;
        }

        return new(
            payloadReplayHash,
            $"sidecar-{sidecarIndex}",
            sidecar.Payload,
            sidecar.FormatVersion,
            sidecar.Compression,
            sidecar.CompressedLength,
            sidecar.UncompressedLength,
            sidecar.UnitCount);
    }

    private static async Task<HttpResponseMessage> PostReplayBatch(
        HttpClient httpClient,
        ServiceUploadBatch batch,
        CancellationToken ct)
    {
        var compressedRequest = CompressBrotli(SerializeUploadRequest(batch.Request));
        using var content = new ByteArrayContent(compressedRequest);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        content.Headers.ContentEncoding.Add("br");
        content.Headers.ContentLength = compressedRequest.Length;

        return await httpClient.PostAsync(UploadEndpoint, content, ct);
    }

#pragma warning disable CA2000 // MultipartFormDataContent owns nested content after Add.
    private static async Task<HttpResponseMessage> PostSpawnPlaybackBatch(
        HttpClient httpClient,
        ServiceUploadBatch batch,
        CancellationToken ct)
    {
        using var multipart = new MultipartFormDataContent();

        var compressedRequest = CompressGZip(SerializeUploadRequest(batch.Request));
        var requestContent = new ByteArrayContent(compressedRequest);
        requestContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        requestContent.Headers.ContentEncoding.Add("gzip");
        requestContent.Headers.ContentLength = compressedRequest.Length;
        multipart.Add(requestContent, "request", "request.json.gz");

        var manifest = batch.Sidecars
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
            new StringContent(JsonSerializer.Serialize(manifest, UploadJsonOptions), Encoding.UTF8, "application/json"),
            "manifest");

        foreach (var sidecar in batch.Sidecars)
        {
            var sidecarContent = new ByteArrayContent(sidecar.Payload);
            sidecarContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            sidecarContent.Headers.ContentLength = sidecar.Payload.Length;
            multipart.Add(sidecarContent, sidecar.PartName, $"{sidecar.PartName}.bin");
        }

        return await httpClient.PostAsync(SpawnPlaybackUploadEndpoint, multipart, ct);
    }
#pragma warning restore CA2000

    private static async Task<List<string>> GetUploadedHashes(
        HttpResponseMessage response,
        CancellationToken ct)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        var result = await JsonSerializer.DeserializeAsync<ReplayImportBatchResultDto>(
            stream,
            UploadJsonOptions,
            ct);

        return result?.ReplayHashes.Count > 0 ? result.ReplayHashes : [];
    }

    private static List<int> GetUploadedReplayIds(ServiceUploadBatch batch, List<string> uploadedHashes)
    {
        var uploadedHashSet = uploadedHashes.ToHashSet(StringComparer.Ordinal);
        return batch.ReplayHashes
            .Where(replay => uploadedHashSet.Contains(replay.PayloadReplayHash))
            .Select(replay => replay.ReplayId)
            .ToList();
    }

    private static async Task EnsureUploadSuccess(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(ct);
        var message = string.IsNullOrWhiteSpace(body)
            ? $"Upload failed with {(int)response.StatusCode} {response.ReasonPhrase}."
            : $"Upload failed with {(int)response.StatusCode} {response.ReasonPhrase}: {body}";
        throw new HttpRequestException(message, null, response.StatusCode);
    }

    private static List<RequestNames> GetUploadRequestNames(AppOptions config)
    {
        return config.Sc2Profiles
            .Where(x => x.PlayerId.ToonId > 0)
            .Select(s => new RequestNames
            {
                Name = s.Name,
                ToonId = s.PlayerId.ToonId,
                RegionId = s.PlayerId.RegionId,
                RealmId = s.PlayerId.RealmId,
            })
            .ToList();
    }

    private static byte[] SerializeUploadRequest(UploadRequestDto request)
    {
        return JsonSerializer.SerializeToUtf8Bytes(request, UploadJsonOptions);
    }

    private static byte[] CompressGZip(byte[] bytes)
    {
        using var ms = new MemoryStream();
        using (var gz = new GZipStream(ms, CompressionLevel.Fastest, leaveOpen: true))
        {
            gz.Write(bytes, 0, bytes.Length);
        }

        return ms.ToArray();
    }

    private static byte[] CompressBrotli(byte[] bytes)
    {
        using var ms = new MemoryStream();
        using (var br = new BrotliStream(ms, CompressionLevel.Fastest, leaveOpen: true))
        {
            br.Write(bytes, 0, bytes.Length);
        }

        return ms.ToArray();
    }

    private sealed record UploadReplayCandidate(int ReplayId, bool HasSidecar);

    private sealed record ServiceUploadBatch(
        UploadRequestDto Request,
        List<int> ReplayIds,
        List<UploadReplayHash> ReplayHashes,
        List<ServiceUploadSidecar> Sidecars);

    private sealed record UploadReplayHash(int ReplayId, string PayloadReplayHash);

    private sealed record ServiceUploadSidecar(
        string ReplayHash,
        string PartName,
        byte[] Payload,
        ushort FormatVersion,
        SpawnPlaybackCompression Compression,
        int CompressedLength,
        int UncompressedLength,
        int UnitCount);
}
