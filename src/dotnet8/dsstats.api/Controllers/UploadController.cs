using dsstats.api.Services;
using dsstats.shared;
using Microsoft.AspNetCore.Mvc;

namespace dsstats.api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[ServiceFilter(typeof(AuthenticationFilterAttribute))]
public class UploadController : Controller
{
    private readonly UploadService uploadService;

    public UploadController(UploadService uploadService)
    {
        this.uploadService = uploadService;
    }

    [HttpPost]
    [RequestSizeLimit(1024000000)]
    [Route("ImportReplays")]
    public async Task<ActionResult> ImportReplays([FromBody] UploadDto uploadDto)
    {
        var success = await uploadService.Upload(uploadDto);
        if (success)
        {
            return Ok();
        }
        return BadRequest();
    }
}
