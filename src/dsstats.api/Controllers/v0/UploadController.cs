using dsstats.api.Services;
using dsstats.shared;
using Microsoft.AspNetCore.Mvc;

namespace dsstats.api.Controllers.v0;

[ApiController]
[Route("api8/v0/[controller]")]
[ServiceFilter(typeof(AuthenticationFilterAttributeV6))]
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
    public async Task<ActionResult> ImportReplays([FromBody] UploadDtoV6 uploadDtoV6)
    {
        (var appVersion, var requestNames) = await uploadService.GetRequestNames(uploadDtoV6.AppGuid);

        UploadDto uploadDto = new()
        {
            AppGuid = uploadDtoV6.AppGuid,
            RequestNames = requestNames,
            AppVersion = appVersion,
            Base64ReplayBlob = uploadDtoV6.Base64ReplayBlob
        };

        var success = await uploadService.Upload(uploadDto);
        if (success)
        {
            return Ok();
        }
        return BadRequest();
    }
}
