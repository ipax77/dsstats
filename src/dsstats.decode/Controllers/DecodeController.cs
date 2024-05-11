using Microsoft.AspNetCore.Mvc;

namespace dsstats.decode;

[ApiController]
[Route("/api/v1/[controller]")]
public class DecodeController(DecodeService decodeService, ILogger<DecodeController> logger) : Controller
{
    [HttpPost]
    [RequestSizeLimit(15728640)]
    [Route("upload/{guid}")]
    public async Task<ActionResult<int>> UploadReplays(string guid, [FromForm] List<IFormFile> files)
    {
        logger.LogInformation("indahouse1 {guid}", guid);
        if (Guid.TryParse(guid, out var fileGuid))
        {
            var queueCount = await decodeService.SaveReplays(fileGuid, files);
            logger.LogInformation("indahouse2, {count}", queueCount);
            if (queueCount >= 0)
            {
                return Ok(queueCount);
            }
            else
            {
                return StatusCode(500);
            }
        }
        return BadRequest();
    }
}