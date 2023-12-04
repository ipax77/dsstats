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
        var replay = await replaysService.GetReplay(hash, dry, token);
        if (replay is null)
        {
            return NotFound();
        }
        return replay;
    }

    [HttpGet]
    [Route("replayrating/{combo}/{hash}")]
    public async Task<ActionResult<ReplayRatingDto?>> GetReplayRating(bool combo, string hash)
    {
        var rating = await replaysService.GetReplayRating(hash, combo);
        if (rating is null)
        {
            return NotFound();
        }
        return rating;
    }

    [HttpGet]
    [Route("arcadereplay/{hash}")]
    public async Task<ActionResult<ArcadeReplayDto?>> GetArcadeReplay(string hash, CancellationToken token = default)
    {
        var replay = await replaysService.GetArcadeReplay(hash, token);
        if (replay is null)
        {
            return NotFound();
        }
        return replay;
    }
}
