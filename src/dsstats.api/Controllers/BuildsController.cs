using dsstats.shared;
using dsstats.shared.Interfaces;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace dsstats.api.Controllers;

[EnableCors("dsstatsOrigin")]
[ApiController]
[Route("api8/v1/[controller]")]
public class BuildsController : Controller
{
    private readonly IBuildService buildService;
    private readonly IUnitmapService unitmapService;

    public BuildsController(IBuildService buildService, IUnitmapService unitmapService)
    {
        this.buildService = buildService;
        this.unitmapService = unitmapService;
    }

    [HttpPost]
    public async Task<ActionResult<BuildResponse>> GetBuild(BuildRequest request, CancellationToken token = default)
    {
        return await buildService.GetBuild(request, token);
    }

    [HttpGet]
    [Route("defaultplayers")]
    public async Task<ActionResult<List<RequestNames>>> GetDefaultPlayers()
    {
        return await buildService.GetDefaultPlayers();
    }

    [HttpGet]
    [Route("topplayers/{ratingType:int}")]
    public async Task<ActionResult<List<RequestNames>>> GetTopPlayers(int ratingType)
    {
        return await buildService.GetTopPlayers((RatingType)ratingType);
    }

    [HttpPost]
    [Route("replayscount")]
    public async Task<int> GetReplaysCount(BuildRequest request, CancellationToken token = default)
    {
        return await buildService.GetReplaysCount(request, token);
    }

    [HttpPost]
    [Route("replays/{skip:int}/{take:int}")]
    public async Task<List<ReplayListDto>> GetReplays([FromBody] BuildRequest request, int skip, int take, CancellationToken token)
    {
        return await buildService.GetReplays(request, skip, take, token);
    }

    [HttpPost]
    [Route("unitmap")]
    public async Task<ActionResult<Unitmap>> GetUnitmap(UnitmapRequest request, CancellationToken token = default)
    {
        return await unitmapService.GetUnitMap(request);
    }

    [HttpPost]
    [Route("replaybuildmap")]
    public async Task<ActionResult<BuildMapResponse>> GetReplayBuildMap(BuildRequest request, CancellationToken token = default)
    {
        return await buildService.GetReplayBuildMap(request, token);
    }
}
