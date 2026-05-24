using dsstats.api.Services;
using dsstats.dbServices;
using dsstats.shared;
using dsstats.shared.Interfaces;
using dsstats.shared.Upload;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.IO.Compression;
using System.Text.Json;

namespace dsstats.api.Controllers;

[ApiController]
[Route("api10/[controller]")]
[Route("api8/v1/[controller]")] // DEBUG
[ServiceFilter(typeof(AuthenticationFilterAttribute))]
public class UploadController(
    UploadService uploadService,
    IImportService importService,
    ILogger<UploadController> logger) : Controller
{
    private static readonly JsonSerializerOptions UploadJsonOptions = new(JsonSerializerDefaults.Web);

    [HttpPost]
    [RequestSizeLimit(1024000000)]
    [Route("ImportReplays")]
    public async Task<ActionResult> ImportReplays([FromBody] UploadDto uploadDto)
    {
        var success = await uploadService.ProcessUploadAsync(uploadDto);
        if (success)
        {
            return Ok();
        }
        return BadRequest();
    }

    [HttpPost]
    [RequestSizeLimit(1024000000)]
    public async Task<ActionResult> Upload([FromBody] UploadRequestDto request)
    {
        var success = await uploadService.ProcessUploadAsync(request);
        if (success)
        {
            return Ok();
        }
        return BadRequest();
    }

    [HttpPost]
    [RequestSizeLimit(3 * 1024 * 1024)]
    [Route("uploadreplay/{guid}")]
    [EnableRateLimiting("fixed")]
    public async Task<ActionResult<DecodeRequestResult>> UploadReplay(string guid, [FromForm] IFormFile file)
    {
        logger.LogInformation("indahouse");
        if (Guid.TryParse(guid, out var fileGuid))
        {
            var result = await uploadService.SaveReplay(fileGuid, file);
            if (result.Success)
            {
                return Ok(result);
            }
            else
            {
                return BadRequest(result.Error);
            }
        }
        return BadRequest();
    }

    [HttpPost]
    [RequestSizeLimit(10 * 1024 * 1024)]
    [Route("import-spawn-playback")]
    public async Task<ActionResult<ReplayImportResultDto>> ImportSpawnPlayback(
        [FromForm] string replay,
        [FromForm] IFormFile sidecar,
        [FromForm] ushort formatVersion,
        [FromForm] SpawnPlaybackCompression compression,
        [FromForm] int compressedLength,
        [FromForm] int uncompressedLength,
        [FromForm] int unitCount,
        CancellationToken token)
    {
        if (sidecar.Length == 0)
        {
            return BadRequest("Invalid sidecar payload.");
        }

        var replayDto = JsonSerializer.Deserialize<ReplayDto>(replay);
        if (replayDto is null)
        {
            return BadRequest("Invalid replay payload.");
        }

        using var payload = new MemoryStream((int)sidecar.Length);
        await sidecar.CopyToAsync(payload, token);
        var bytes = payload.ToArray();
        if (bytes.Length != compressedLength)
        {
            return BadRequest("Sidecar compressed length does not match payload length.");
        }

        var encodedSidecar = new SpawnPlaybackEncodedSidecar(
            bytes,
            compressedLength,
            uncompressedLength,
            unitCount,
            formatVersion,
            compression);

        await importService.InsertReplayImports([new(replayDto, encodedSidecar)]);
        return Ok(new ReplayImportResultDto
        {
            Success = true,
            ReplayHash = replayDto.ComputeHash()
        });
    }

    [HttpPost]
    [RequestSizeLimit(1024 * 1024 * 1024)]
    [Route("import-spawn-playbacks")]
    public async Task<ActionResult<ReplayImportBatchResultDto>> ImportSpawnPlaybacks(
        [FromForm] IFormFile request,
        [FromForm] string manifest,
        CancellationToken token)
    {
        if (request.Length == 0)
        {
            return BadRequest("Invalid replay payload.");
        }

        UploadRequestDto? uploadRequest;
        await using (var requestStream = request.OpenReadStream())
        await using (var gzip = new GZipStream(requestStream, CompressionMode.Decompress))
        {
            uploadRequest = await JsonSerializer.DeserializeAsync<UploadRequestDto>(
                gzip,
                UploadJsonOptions,
                token);
        }

        if (uploadRequest is null)
        {
            return BadRequest("Invalid replay payload.");
        }

        var manifestEntries = JsonSerializer.Deserialize<List<SpawnPlaybackUploadManifestEntryDto>>(
            manifest,
            UploadJsonOptions) ?? [];

        var entriesByReplayHash = new Dictionary<string, SpawnPlaybackUploadManifestEntryDto>(StringComparer.Ordinal);
        foreach (var entry in manifestEntries)
        {
            if (string.IsNullOrWhiteSpace(entry.ReplayHash)
                || string.IsNullOrWhiteSpace(entry.PartName))
            {
                return BadRequest("Invalid sidecar manifest.");
            }

            if (!entriesByReplayHash.TryAdd(entry.ReplayHash, entry))
            {
                return BadRequest("Duplicate sidecar manifest entry.");
            }

            if (entry.FormatVersion != SpawnPlaybackSidecarCodec.FormatVersion
                || entry.Compression != SpawnPlaybackSidecarCodec.Compression
                || entry.CompressedLength <= 0
                || entry.UncompressedLength <= 0
                || entry.UnitCount <= 0)
            {
                return BadRequest("Invalid sidecar metadata.");
            }
        }

        var filesByPartName = new Dictionary<string, IFormFile>(StringComparer.Ordinal);
        foreach (var file in HttpContext.Request.Form.Files)
        {
            if (string.Equals(file.Name, "request", StringComparison.Ordinal))
            {
                continue;
            }

            if (!filesByPartName.TryAdd(file.Name, file))
            {
                return BadRequest("Duplicate sidecar payload.");
            }
        }

        List<ReplayImportDto> imports = new(uploadRequest.Replays.Count);
        List<string> replayHashes = new(uploadRequest.Replays.Count);
        foreach (var replay in uploadRequest.Replays)
        {
            var replayHash = replay.ComputeHash();
            replayHashes.Add(replayHash);

            SpawnPlaybackEncodedSidecar? sidecar = null;
            if (entriesByReplayHash.Remove(replayHash, out var entry))
            {
                if (!filesByPartName.TryGetValue(entry.PartName, out var sidecarFile))
                {
                    return BadRequest("Missing sidecar payload.");
                }
                if (sidecarFile.Length != entry.CompressedLength)
                {
                    return BadRequest("Sidecar compressed length does not match payload length.");
                }

                var bytes = new byte[checked((int)sidecarFile.Length)];
                await using (var sidecarStream = sidecarFile.OpenReadStream())
                {
                    await sidecarStream.ReadExactlyAsync(bytes, token);
                }

                sidecar = new(
                    bytes,
                    entry.CompressedLength,
                    entry.UncompressedLength,
                    entry.UnitCount,
                    entry.FormatVersion,
                    entry.Compression);

                replay.SpawnPlayback = new()
                {
                    Available = true,
                    FormatVersion = entry.FormatVersion,
                    CompressedLength = entry.CompressedLength,
                    UncompressedLength = entry.UncompressedLength,
                    UnitCount = entry.UnitCount,
                };
            }

            imports.Add(new(replay, sidecar));
        }

        if (entriesByReplayHash.Count > 0)
        {
            return BadRequest("Sidecar manifest contains a replay hash that is not in the upload request.");
        }

        await importService.InsertReplayImports(imports);
        return Ok(new ReplayImportBatchResultDto
        {
            Success = true,
            ReplayHashes = replayHashes,
        });
    }
}
