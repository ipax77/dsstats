using dsstats.api.Services;
using dsstats.shared;
using Microsoft.AspNetCore.Mvc;

namespace dsstats.api.Controllers;

[ApiController]
[Route("api8/v1/[controller]")]
[ServiceFilter(typeof(AuthenticationFilterAttribute))]
public class UploadController : Controller
{
    private readonly UploadService uploadService;

    public UploadController(UploadService uploadService)
    {
        this.uploadService = uploadService;
    }

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
}
