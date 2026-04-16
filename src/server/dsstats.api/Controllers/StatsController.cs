using dsstats.shared;
using dsstats.shared.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace dsstats.api.Controllers;

[ApiController]
[Route("api10/[controller]")]
public class StatsController(IStatsService statsService, IDashboardStatsService dashboardStatsService) : Controller
{
    [HttpPost]
    public async Task<IActionResult> GetStats([FromBody] StatsRequest request, CancellationToken token = default)
    {
        IStatsResponse response;
        switch (request.Type)
        {
            case StatsType.Winrate:
                response = await statsService.GetStatsAsync<WinrateResponse>(request.Type, request, token);
                break;
            case StatsType.Synergy:
                response = await statsService.GetStatsAsync<SynergyResponse>(request.Type, request, token);
                break;
            case StatsType.Timeline:
                response = await statsService.GetStatsAsync<TimelineResponse>(request.Type, request, token);
                break;
            case StatsType.Stats:
                response = await statsService.GetStatsAsync<StatsResponse>(request.Type, request, token);
                break;
            default:
                return BadRequest("Invalid stats type");
        }
        return Ok(response);
    }

    [HttpPost]
    [Route("user")]
    public async Task<IActionResult> GetUserStats([FromBody] UserStatsRequest request, CancellationToken token = default)
    {
        IStatsResponse response;
        switch (request.Request.Type)
        {
            case StatsType.Winrate:
                response = await statsService.GetUserStatsAsync<WinrateResponse>(request.Request.Type, request.Request, request.ToonId, token);
                break;
            case StatsType.Synergy:
                response = await statsService.GetUserStatsAsync<SynergyResponse>(request.Request.Type, request.Request, request.ToonId, token);
                break;
            case StatsType.Timeline:
                response = await statsService.GetUserStatsAsync<TimelineResponse>(request.Request.Type, request.Request, request.ToonId, token);
                break;
            case StatsType.Stats:
                response = await statsService.GetUserStatsAsync<StatsResponse>(request.Request.Type, request.Request, request.ToonId, token);
                break;
            default:
                return BadRequest("Invalid stats type");
        }
        return Ok(response);
    }

    [HttpGet]
    public async Task<IActionResult> GetDashboardStats(CancellationToken token = default)
    {
        var stats = await dashboardStatsService.GetDashboardStatsAsync(token);
        return Ok(stats);
    }
}
