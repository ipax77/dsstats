using dsstats.api.Services;
using dsstats.dbServices.BuildDetails;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace dsstats.api.Controllers;

[ApiController]
[Route("api10/[controller]")]
public class AdminController(
    ArcadeJobService arcadeJobService,
    BuildDetailGenerationService buildDetailGenerationService,
    IConfiguration configuration) : Controller
{
    [HttpPost("trigger-arcade-job")]
    [EnableRateLimiting("admin")]
    public IActionResult TriggerArcadeJob()
    {
        if (!IsAuthorized())
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

    [HttpPost("trigger-build-detail-generation")]
    [EnableRateLimiting("admin")]
    public IActionResult TriggerBuildDetailGeneration()
    {
        if (!IsAuthorized())
        {
            return Unauthorized();
        }

        if (!buildDetailGenerationService.TryStartFullRun())
        {
            return Conflict(new { started = false, reason = "already running" });
        }

        return Ok(new { started = true });
    }

    private bool IsAuthorized()
    {
        var expectedKey = configuration["dsstats:AdminAuth"];
        var providedKey = Request.Headers.Authorization.SingleOrDefault();

        return !string.IsNullOrEmpty(expectedKey) && providedKey == expectedKey;
    }
}
