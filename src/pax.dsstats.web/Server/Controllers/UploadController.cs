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
    private readonly IHttpClientFactory httpClientFactory;
    private readonly ILogger<UploadController> logger;
    private readonly bool forwardToDev = true;

    public UploadController(UploadService uploadService, IHttpClientFactory httpClientFactory, ILogger<UploadController> logger)
    {
        this.uploadService = uploadService;
        this.httpClientFactory = httpClientFactory;
        this.logger = logger;
    }

    [HttpPost]
    [Route("GetLatestReplayDate")]
    public async Task<ActionResult<DateTime>> GetLatestReplayDate(UploaderDto uploaderDto)
    {
        var latestReplay = await uploadService.CreateOrUpdateUploader(uploaderDto, forwardToDev);
        if (latestReplay == null)
        {
            return Unauthorized();
        }
        else
        {
            return latestReplay.Value;
        }
    }

    [HttpPost]
    [RequestSizeLimit(1024000000)]
    [Route("ImportReplays")]
    public async Task<ActionResult> ImportReplays([FromBody] UploadDto uploadDto)
    {
        if (forwardToDev)
        {
            try
            {
                var httpClient = httpClientFactory.CreateClient("dev");
                (var appVersion, var requestNames) = await uploadService.GetRequestNames(uploadDto.AppGuid);

                UploadDevDto uploadDevDto = new()
                {
                    AppGuid = uploadDto.AppGuid,
                    RequestNames = requestNames,
                    AppVersion = appVersion,
                    Base64ReplayBlob = uploadDto.Base64ReplayBlob
                };

                var result = await httpClient.PostAsJsonAsync("api/v1/Upload/ImportReplays", uploadDevDto);
                result.EnsureSuccessStatusCode();
                return Ok();
            }
            catch (Exception ex)
            {
                logger.LogError("failed forwarding to dev: {error}", ex.Message);
                return StatusCode(500);
            }
        }
        else
        {
            var result = await uploadService.ImportReplays(uploadDto.Base64ReplayBlob, uploadDto.AppGuid, uploadDto.LatestReplays);
            if (result)
            {
                return Ok();
            }
            else
            {
                return BadRequest();
            }
        }
    }

    [HttpGet]
    [Route("DisableUploader/{appGuid}")]
    public async Task<ActionResult<DateTime>> DisableUploader(Guid appGuid)
    {
        return await uploadService.DisableUploader(appGuid);
    }

    [HttpGet]
    [Route("DeleteUploader/{appGuid}")]
    public async Task<ActionResult<bool>> DeleteUploader(Guid appGuid)
    {
        return await uploadService.DeleteUploader(appGuid);
    }


}
