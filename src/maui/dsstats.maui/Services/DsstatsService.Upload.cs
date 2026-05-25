using dsstats.db;
using dsstats.maui.Services.Models;
using dsstats.shared;
using dsstats.shared.Upload;
using Microsoft.EntityFrameworkCore;
using System.IO.Compression;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace dsstats.maui.Services;

public partial class DsstatsService
{
    private const int SpawnPlaybackUploadBatchSize = 50;
    private const int ReplayOnlyUploadBatchSize = 250;
    private static readonly JsonSerializerOptions UploadJsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<UploadResult> Upload(ImportState importState, bool force = false, CancellationToken token = default)
    {
        var result = await StartUpload(importState, force, token).ConfigureAwait(false);
        return result;
    }

    private async Task<UploadResult> StartUpload(ImportState importState, bool force = false, CancellationToken token = default)
    {
        var config = await GetConfig().ConfigureAwait(false);
        if (!config.UploadCredential && !force)
        {
            _uploadStatus = UploadStatus.Forbidden;
            importState.UpdateProgress(importState.Progress with
            {
                UploadStatus = _uploadStatus,
            });
            return new();
        }

        _uploadStatus = UploadStatus.Uploading;
        importState.UpdateProgress(importState.Progress with
        {
            UploadStatus = _uploadStatus,
        });

        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();
        var httpClient = httpClientFactory.CreateClient("api");
        httpClient.DefaultRequestHeaders.Authorization = new("DS8upload77");
        var requestNames = config.Sc2Profiles
            .Where(x => x.ToonId.Id > 0)
            .Select(s => new RequestNames
            {
                Name = s.Name,
                ToonId = s.ToonId.Id,
                RegionId = s.ToonId.Region,
                RealmId = s.ToonId.Realm,
            })
            .ToList();

        try
        {
            while (!token.IsCancellationRequested)
            {
                var candidates = await GetUploadCandidates(context, token).ConfigureAwait(false);
                if (candidates.Count == 0)
                {
                    break;
                }

                var batchCandidates = SelectUploadBatchCandidates(candidates);
                var replays = await GetUploadReplays(
                    context,
                    batchCandidates.Select(candidate => candidate.ReplayId).ToList(),
                    token).ConfigureAwait(false);

                if (replays.Count == 0)
                {
                    break;
                }

                var batch = CreateUploadBatch(config.AppGuid, requestNames, replays);
                using var response = batch.Sidecars.Count > 0
                    ? await PostSpawnPlaybackBatch(httpClient, batch, token).ConfigureAwait(false)
                    : await PostReplayBatch(httpClient, batch, token).ConfigureAwait(false);
                await EnsureUploadSuccess(response, token).ConfigureAwait(false);

                if (batch.Sidecars.Count > 0)
                {
                    var uploadedHashes = await GetUploadedHashes(response, token).ConfigureAwait(false);
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
                        .ExecuteUpdateAsync(e => e.SetProperty(p => p.Uploaded, true), token)
                        .ConfigureAwait(false);
                }
                else
                {
                    await context.Replays
                        .Where(x => batch.ReplayIds.Contains(x.ReplayId))
                        .ExecuteUpdateAsync(e => e.SetProperty(p => p.Uploaded, true), token)
                        .ConfigureAwait(false);
                }
            }

            _uploadStatus = UploadStatus.Success;
            importState.UpdateProgress(importState.Progress with
            {
                UploadStatus = _uploadStatus,
            });
            return new() { Success = true };
        }
        catch (OperationCanceledException)
        {
            _uploadStatus = UploadStatus.None;
            return new() { Success = true };
        }
        catch (Exception ex)
        {
            _uploadStatus = UploadStatus.Failed;
            importState.UpdateProgress(importState.Progress with
            {
                UploadStatus = _uploadStatus,
            });
            return new()
            {
                Error = ex.Message,
            };
        }
    }

    private static async Task<List<UploadReplayCandidate>> GetUploadCandidates(
        DsstatsContext context,
        CancellationToken token)
    {
        return await context.Replays
            .Where(x => !x.Uploaded)
            .AsNoTracking()
            .OrderByDescending(o => o.Gametime)
            .Select(x => new UploadReplayCandidate(x.ReplayId, x.SpawnPlayback != null))
            .Take(ReplayOnlyUploadBatchSize)
            .ToListAsync(token)
            .ConfigureAwait(false);
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
        CancellationToken token)
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
            .ToListAsync(token)
            .ConfigureAwait(false);
    }

    private static MauiUploadBatch CreateUploadBatch(
        Guid appGuid,
        List<RequestNames> requestNames,
        List<Replay> replays)
    {
        List<int> replayIds = new(replays.Count);
        List<UploadReplayHash> replayHashes = new(replays.Count);
        List<ReplayDto> replayDtos = new(replays.Count);
        List<MauiUploadSidecar> sidecars = [];

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
                AppVersion = "ma3.7",
                RequestNames = requestNames,
                Replays = replayDtos,
            },
            replayIds,
            replayHashes,
            sidecars);
    }

    private static MauiUploadSidecar? CreateUploadSidecar(
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
        MauiUploadBatch batch,
        CancellationToken token)
    {
        var compressedRequest = CompressBrotli(SerializeUploadRequest(batch.Request));
        using var content = new ByteArrayContent(compressedRequest);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        content.Headers.ContentEncoding.Add("br");
        content.Headers.ContentLength = compressedRequest.Length;

        return await httpClient.PostAsync("api10/Upload", content, token).ConfigureAwait(false);
    }

    private static async Task<HttpResponseMessage> PostSpawnPlaybackBatch(
        HttpClient httpClient,
        MauiUploadBatch batch,
        CancellationToken token)
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

        return await httpClient.PostAsync("api10/upload/import-spawn-playbacks", multipart, token).ConfigureAwait(false);
    }

    private static async Task<List<string>> GetUploadedHashes(
        HttpResponseMessage response,
        CancellationToken token)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(token).ConfigureAwait(false);
        var result = await JsonSerializer.DeserializeAsync<ReplayImportBatchResultDto>(
            stream,
            UploadJsonOptions,
            token).ConfigureAwait(false);

        return result?.ReplayHashes.Count > 0 ? result.ReplayHashes : [];
    }

    private static List<int> GetUploadedReplayIds(MauiUploadBatch batch, List<string> uploadedHashes)
    {
        var uploadedHashSet = uploadedHashes.ToHashSet(StringComparer.Ordinal);
        return batch.ReplayHashes
            .Where(replay => uploadedHashSet.Contains(replay.PayloadReplayHash))
            .Select(replay => replay.ReplayId)
            .ToList();
    }

    private static async Task EnsureUploadSuccess(HttpResponseMessage response, CancellationToken token)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);
        var message = string.IsNullOrWhiteSpace(body)
            ? $"Upload failed with {(int)response.StatusCode} {response.ReasonPhrase}."
            : $"Upload failed with {(int)response.StatusCode} {response.ReasonPhrase}: {body}";
        throw new HttpRequestException(message, null, response.StatusCode);
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

    private sealed record MauiUploadBatch(
        UploadRequestDto Request,
        List<int> ReplayIds,
        List<UploadReplayHash> ReplayHashes,
        List<MauiUploadSidecar> Sidecars);

    private sealed record UploadReplayHash(int ReplayId, string PayloadReplayHash);

    private sealed record MauiUploadSidecar(
        string ReplayHash,
        string PartName,
        byte[] Payload,
        ushort FormatVersion,
        SpawnPlaybackCompression Compression,
        int CompressedLength,
        int UncompressedLength,
        int UnitCount);
}
