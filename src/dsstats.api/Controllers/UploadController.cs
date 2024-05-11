using dsstats.api.Services;
using dsstats.shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace dsstats.api.Controllers;

[ApiController]
[Route("api8/v1/[controller]")]
[ServiceFilter(typeof(AuthenticationFilterAttribute))]
public class UploadController(UploadService uploadService,
                              DecodeService decodeService,
                              IHttpClientFactory httpClientFactory,
                              ILogger<UploadController> logger) : Controller
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
        logger.LogWarning("indahouse1 {guid}",  guid);
        if (Guid.TryParse(guid, out var fileGuid))
        {
            var httpClient = httpClientFactory.CreateClient("decode");
            try
            {
                var formData = new MultipartFormDataContent();

                foreach (var file in files)
                {
                    var fileContent = new StreamContent(file.OpenReadStream());
                    formData.Add(fileContent, "files", file.FileName);
                }

                var result = await httpClient.PostAsync($"/api/v1/decode/upload/{fileGuid}", formData);
                result.EnsureSuccessStatusCode();
                return Ok(0);
            }
            catch (Exception ex)
            {
                logger.LogError("failed passing decode request: {error}", ex.Message);
            }
        }
        return BadRequest();
    }

    [HttpPost]
    [Route("decoderesult/{guid}")]
    public async Task<ActionResult> DecodeResult(string guid, [FromBody] List<IhReplay> replays)
    {
        logger.LogWarning("got decode result for {guid}", guid);
        if (Guid.TryParse(guid, out var groupId))
        {
            return Ok();
        }
        return BadRequest();
    }
}
