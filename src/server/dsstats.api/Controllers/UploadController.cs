using dsstats.api.Services;
using dsstats.dbServices;
using dsstats.shared;
using dsstats.shared.Interfaces;
using dsstats.shared.Upload;
using Microsoft.AspNetCore.Http.Features;
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
        if (formatVersion != SpawnPlaybackSidecarCodec.FormatVersion
            || compressedLength <= 0
            || uncompressedLength <= 0
            || unitCount <= 0)
        {
            return BadRequest("Invalid sidecar metadata.");
        }
        if (!ImportService.IsSupportedCompression(compression))
        {
            return BadRequest("Unsupported sidecar compression.");
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
    [RequestFormLimits(
        MultipartBodyLengthLimit = 1024L * 1024 * 1024,
        ValueLengthLimit = 10 * 1024 * 1024,
        MultipartHeadersLengthLimit = 128 * 1024,
        MultipartHeadersCountLimit = 256)]
    [Route("import-spawn-playbacks")]
    public async Task<ActionResult<ReplayImportBatchResultDto>> ImportSpawnPlaybacks(
        [FromForm] IFormFile? request,
        [FromForm] string? manifest,
        CancellationToken token)
    {
        if (request is null || request.Length == 0)
        {
            return BadRequest("Invalid replay payload.");
        }

        UploadRequestDto? uploadRequest;
        try
        {
            await using var requestStream = request.OpenReadStream();
            await using var gzip = new GZipStream(requestStream, CompressionMode.Decompress);
            uploadRequest = await JsonSerializer.DeserializeAsync<UploadRequestDto>(
                gzip,
                UploadJsonOptions,
                token);
        }
        catch (InvalidDataException ex)
        {
            logger.LogWarning(ex, "Failed to decompress spawn playback import request.");
            return BadRequest("Invalid replay payload gzip stream.");
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Failed to deserialize spawn playback import request.");
            return BadRequest("Invalid replay payload json.");
        }

        if (uploadRequest is null)
        {
            return BadRequest("Invalid replay payload.");
        }

        if (string.IsNullOrWhiteSpace(manifest))
        {
            return BadRequest("Missing sidecar manifest.");
        }

        List<SpawnPlaybackUploadManifestEntryDto> manifestEntries;
        try
        {
            manifestEntries = JsonSerializer.Deserialize<List<SpawnPlaybackUploadManifestEntryDto>>(
                manifest,
                UploadJsonOptions) ?? [];
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Failed to deserialize spawn playback import manifest.");
            return BadRequest("Invalid sidecar manifest json.");
        }

        var payloadsByPartName = new Dictionary<string, SpawnPlaybackUploadPayload>(StringComparer.Ordinal);
        foreach (var file in HttpContext.Request.Form.Files)
        {
            if (string.Equals(file.Name, "request", StringComparison.Ordinal))
            {
                continue;
            }

            var payload = new SpawnPlaybackUploadPayload(
                file.Name,
                file.Length,
                file.OpenReadStream);

            if (!payloadsByPartName.TryAdd(file.Name, payload))
            {
                return BadRequest("Duplicate sidecar payload.");
            }
        }

        var result = await importService.InsertReplayImportsWithSidecars(
            uploadRequest,
            manifestEntries,
            payloadsByPartName,
            token);

        if (!result.Success)
        {
            return BadRequest(result.Error);
        }

        return Ok(result);
    }
}
