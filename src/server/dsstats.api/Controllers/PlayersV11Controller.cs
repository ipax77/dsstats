using dsstats.shared;
using dsstats.shared.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace dsstats.api.Controllers;

[ApiController]
[Route("api11/Players")]
[EnableRateLimiting("fixed")]
public sealed class PlayersV11Controller(IPlayerProfileService profiles) : ControllerBase
{
    [HttpPost("overview")]
    public async Task<IActionResult> Overview(PlayerProfileRequest request, CancellationToken token)
    {
        var result = await profiles.GetOverview(request, token);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("details")]
    public async Task<IActionResult> Details(PlayerProfileRequest request, CancellationToken token)
    {
        var result = await profiles.GetDetails(request, token);
        return result is null ? NotFound() : Ok(result);
    }
}
