using dsstats.api.Services;
using dsstats.dbServices;
using dsstats.shared;
using dsstats.shared.Interfaces;
using dsstats.shared.Upload;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
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
}
