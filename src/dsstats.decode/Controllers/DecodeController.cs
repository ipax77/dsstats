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
        if (Guid.TryParse(guid, out var fileGuid))
        {
            var queueCount = await decodeService.SaveReplays(fileGuid, files);
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