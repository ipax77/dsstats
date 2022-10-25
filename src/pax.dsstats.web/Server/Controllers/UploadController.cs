using Microsoft.AspNetCore.Mvc;
using pax.dsstats.shared;
using pax.dsstats.web.Server.Attributes;
using pax.dsstats.web.Server.Services;

namespace pax.dsstats.web.Server.Controllers;

[ServiceFilter(typeof(AuthenticationFilterAttribute))]
[ApiController]
[Route("api/[controller]")]
public class UploadController : ControllerBase
{
    private readonly UploadService uploadService;

    public UploadController(UploadService uploadService)
    {
        this.uploadService = uploadService;
    }

    [HttpPost]
    [Route("GetLatestReplayDate")]
    public async Task<ActionResult<DateTime>> GetLatestReplayDate(UploaderDto uploaderDto)
    {
        return await uploadService.CreateOrUpdateUploader(uploaderDto);
    }

    [HttpPost]
    [RequestSizeLimit(1024000000)]
    [Route("ImportReplays/{appGuid}")]
    public async Task<ActionResult> ImportReplays([FromBody] string base64string, Guid appGuid)
    {
        await uploadService.ImportReplays(base64string, appGuid);
        return Ok();
    }
}
