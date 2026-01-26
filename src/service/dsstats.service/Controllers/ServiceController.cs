using dsstats.service.Services;
using Microsoft.AspNetCore.Mvc;

namespace dsstats.service.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ServiceController(IDsstatsService dsstatsService, IHostApplicationLifetime appLifetime, ILogger<ServiceController> logger) : ControllerBase
{
    [HttpPost("trigger-import")]
    public async Task<IActionResult> TriggerImport()
    {
        logger.LogInformation("Manual import triggered via API.");
        await dsstatsService.StartImportAsync(CancellationToken.None);
        return Accepted();
    }

    [HttpPost("stop")]
    public IActionResult StopService()
    {
        logger.LogInformation("Service stop requested via API.");
        appLifetime.StopApplication();
        return Ok("Service is stopping.");
    }
}
