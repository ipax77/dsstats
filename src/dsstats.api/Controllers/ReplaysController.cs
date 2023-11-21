using dsstats.shared;
using dsstats.shared.Interfaces;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace dsstats.api.Controllers;

[EnableCors("dsstatsOrigin")]
[ApiController]
[Route("api8/v1/[controller]")]
public class ReplaysController : Controller
{
    private readonly IReplaysService replaysService;

    public ReplaysController(IReplaysService replaysService)
    {
        this.replaysService = replaysService;
    }

    [HttpPost]
    [Route("count")]
    public async Task<ActionResult<int>> GetReplaysCount(ReplaysRequest request, CancellationToken token = default)
    {
        return await replaysService.GetReplaysCount(request, token);
    }

    [HttpPost]
    [Route("replays")]
    public async Task<ActionResult<ReplaysResponse>> GetReplays(ReplaysRequest request, CancellationToken token)
    {
        return await replaysService.GetReplays(request, token);
    }

    [HttpGet]
    [Route("replay/{dry}/{hash}")]
    public async Task<ActionResult<ReplayDto?>> GetReplay(bool dry, string hash, CancellationToken token = default)
    {
        return await replaysService.GetReplay(hash, dry, token);
    }

    [HttpGet]
    [Route("replayrating/{combo}/{hash}")]
    public async Task<ActionResult<ReplayRatingDto?>> GetReplayRating(bool combo, string hash)
    {
        return await replaysService.GetReplayRating(hash, combo);
    }

    [HttpGet]
    [Route("arcadereplay/{hash}")]
    public async Task<ArcadeReplayDto?> GetArcadeReplay(string hash, CancellationToken token = default)
    {
        return await replaysService.GetArcadeReplay(hash, token);
    }
}
