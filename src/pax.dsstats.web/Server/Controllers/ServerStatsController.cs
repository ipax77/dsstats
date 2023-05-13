using Microsoft.AspNetCore.Mvc;
using pax.dsstats.dbng.Services.MergeService;
using pax.dsstats.shared;
using pax.dsstats.shared.Arcade;
using pax.dsstats.shared.Interfaces;

namespace pax.dsstats.web.Server.Controllers;

[ApiController]
[Route("/api/v1/[controller]")]
public class ServerStatsController : Controller
{
    private readonly IServerStatsService serverStatsService;
    private readonly ReplaysMergeService replaysMergeService;

    public ServerStatsController(IServerStatsService serverStatsService, ReplaysMergeService replaysMergeService)
    {
        this.serverStatsService = serverStatsService;
        this.replaysMergeService = replaysMergeService;
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

    [HttpPost]
    [Route("mergeresult")]
    public async Task<ActionResult<MergeResultReplays>> GetMergeResultReplays(PlayerId playerId,
                                                                              CancellationToken token)
    {
        return await replaysMergeService.GetMergeResultReplay(playerId, token);        
    }
}
