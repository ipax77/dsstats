using dsstats.api.Services;
using dsstats.shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace dsstats.api.Controllers;

[ApiController]
[Route("api8/v1/[controller]")]
[ServiceFilter(typeof(AuthenticationFilterAttribute))]
public class UploadController(UploadService uploadService,
                              DecodeService decodeService) : Controller
{
    private readonly UploadService uploadService = uploadService;

    [HttpPost]
    [Route("GetLatestReplayDate")]
    public async Task<ActionResult<DateTime>> GetLatestReplayDate(UploaderDtoV6 uploaderDto)
    {
        await uploadService.UploadPrepV6(uploaderDto);
        return await Task.FromResult(DateTime.UtcNow);
    }

    [HttpPost]
    [RequestSizeLimit(1024000000)]
    [Route("ImportReplays")]
    public async Task<ActionResult> ImportReplays8([FromBody] UploadDto uploadDto)
    {
        var success = await uploadService.Upload(uploadDto);
        if (success)
        {
            return Ok();
        }
        return BadRequest();
    }

    [HttpPost]
    [RequestSizeLimit(15728640)]
    [Route("uploadreplays/{guid}")]
    [EnableRateLimiting("fixed")]
    public async Task<ActionResult<int>> UploadReplays(string guid, [FromForm] List<IFormFile> files)
    {
        if (Guid.TryParse(guid, out var fileGuid))
        {
            await decodeService.SaveReplays(fileGuid, files);
            return Ok();
        }
        return BadRequest();
    }

    [HttpPost]
    [Route("decoderesult/{guid}")]
    public async Task<ActionResult> DecodeResult(string guid, [FromBody] List<IhReplay> replays)
    {
        if (Guid.TryParse(guid, out var groupId))
        {
            await decodeService.ConsumeDecodeResult(groupId, replays);
            return Ok();
        }
        return BadRequest();
    }

    [HttpPost]
    [RequestSizeLimit(15728640)]
    [Route("uploadchallengereplays/{guid}")]
    [EnableRateLimiting("fixed")]
    public async Task<ActionResult<int>> UploadChallengeReplays(string guid, [FromForm] List<IFormFile> files)
    {
        if (Guid.TryParse(guid, out var fileGuid))
        {
            await decodeService.SaveRawReplays(fileGuid, files);
            return Ok();
        }
        return BadRequest();
    }

    [HttpPost]
    [Route("decoderawresult/{guid}")]
    public async Task<ActionResult> DecodeRawResult(string guid, [FromBody] List<ChallengeResponse> challengeResponses)
    {
        if (Guid.TryParse(guid, out var groupId))
        {
            await decodeService.ConsumeRawDecodeResult(groupId, challengeResponses);
            return Ok();
        }
        return BadRequest();
    }
}
