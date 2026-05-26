using dsstats.api.Services;
using dsstats.shared;
using dsstats.shared.Interfaces;
using dsstats.shared.Upload;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace dsstats.api.Controllers;

[ApiController]
[Route("api10/[controller]")]
[ServiceFilter(typeof(AuthenticationFilterAttribute))]
public class UploadController(
    UploadService uploadService) : Controller
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
        var result = await uploadService.ProcessSpawnPlaybackUploadAsync(
            replay,
            sidecar,
            formatVersion,
            compression,
            compressedLength,
            uncompressedLength,
            unitCount,
            token);

        if (!result.Success)
        {
            return BadRequest(result.Error);
        }

        return Ok(result);
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
        var result = await uploadService.ProcessSpawnPlaybackUploadAsync(
            request,
            manifest,
            HttpContext.Request.Form.Files,
            token);

        if (!result.Success)
        {
            return BadRequest(result.Error);
        }

        return Ok(result);
    }
}
