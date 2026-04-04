using dsstats.api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace dsstats.api.Controllers;

[ApiController]
[Route("api10/[controller]")]
public class AdminController(ArcadeJobService arcadeJobService, IConfiguration configuration) : Controller
{
    [HttpPost("trigger-arcade-job")]
    [EnableRateLimiting("admin")]
    public IActionResult TriggerArcadeJob()
    {
        var expectedKey = configuration["dsstats:AdminAuth"];
        var providedKey = Request.Headers["Authorization"].SingleOrDefault();

        if (string.IsNullOrEmpty(expectedKey) || providedKey != expectedKey)
        {
            return Unauthorized();
        }

        if (arcadeJobService.IsRunning)
        {
            return Conflict(new { started = false, reason = "already running" });
        }

        _ = arcadeJobService.RunCalcAsync(CancellationToken.None);

        return Ok(new { started = true });
    }
}
