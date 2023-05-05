using Microsoft.AspNetCore.Mvc;
using pax.dsstats.shared;
using pax.dsstats.shared.Interfaces;

namespace pax.dsstats.web.Server.Controllers;

[ApiController]
[Route("/api/v1/[controller]")]
public class ServerStatsController : Controller
{
    private readonly IServerStatsService serverStatsService;

    public ServerStatsController(IServerStatsService serverStatsService)
    {
        this.serverStatsService = serverStatsService;
    }

    [HttpGet]
    [Route("sc2arcade")]
    public async Task<ActionResult<List<ServerStatsResult>>> GetSc2ArcadeStats()
    {
        return await serverStatsService.GetSc2ArcadeStats();
    }

    [HttpGet]
    [Route("dsstats")]
    public async Task<ActionResult<List<ServerStatsResult>>> GetDsstatsStats()
    {
        return await serverStatsService.GetDsstatsStats();
    }
}
