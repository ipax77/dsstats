using dsstats.api.Services;
using dsstats.shared;
using Microsoft.AspNetCore.Mvc;

namespace dsstats.api.Controllers;

[ApiController]
[Route("api8/v1/[controller]")]
[ServiceFilter(typeof(AuthenticationFilterAttribute))]
public class UploadController(UploadService uploadService, DecodeService decodeService) : Controller
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
    public async Task<IActionResult> UploadReplays(string guid, [FromForm] List<IFormFile> files)
    {
        if (Guid.TryParse(guid, out var fileGuid))
        {
            var sccess = await decodeService.SaveReplays(fileGuid, files);
            if (sccess)
            {
                return Ok();
            }
        }
        return BadRequest();
    }
}
