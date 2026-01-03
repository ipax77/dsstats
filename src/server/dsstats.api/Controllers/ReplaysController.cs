using dsstats.shared;
using dsstats.shared.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace dsstats.api.Controllers;

[ApiController]
[Route("api10/[controller]")]
public class ReplaysController(IReplayRepository replayRepository) : Controller
{
    [EnableRateLimiting("fixed")]
    [HttpGet("{replayHash}")]
    public async Task<IActionResult> GetReplayDetails(string replayHash)
    {
        var replay = await replayRepository.GetReplayDetails(replayHash);
        if (replay == null)
        {
            return NotFound();
        }
        return Ok(replay);
    }

    [EnableRateLimiting("fixed")]
    [HttpGet("rating/{replayHash}")]
    public async Task<IActionResult> GetReplayRating(string replayHash)
    {
        var rating = await replayRepository.GetReplayRating(replayHash);
        if (rating == null)
        {
            return NotFound();
        }
        return Ok(rating);
    }

    [HttpPost("list")]
    public async Task<IActionResult> GetReplays([FromBody] ReplaysRequest request, CancellationToken token = default)
    {
        var replays = await replayRepository.GetReplays(request, token);
        return Ok(replays);
    }

    [HttpPost("listcount")]
    public async Task<IActionResult> GetReplaysCount([FromBody] ReplaysRequest request, CancellationToken token = default)
    {
        var count = await replayRepository.GetReplaysCount(request, token);
        return Ok(count);
    }

    [HttpGet("arcade/{replayHash}")]
    public async Task<IActionResult> GetArcadeReplayDetails(string replayHash)
    {
        var replay = await replayRepository.GetArcadeReplayDetails(replayHash);
        if (replay == null)
        {
            return NotFound();
        }
        return Ok(replay);
    }

    [HttpPost("arcade/list")]
    public async Task<IActionResult> GetArcadeReplays([FromBody] ArcadeReplaysRequest request, CancellationToken token = default)
    {
        var replays = await replayRepository.GetArcadeReplays(request, token);
        return Ok(replays);
    }

    [HttpPost("arcade/listcount")]
    public async Task<IActionResult> GetArcadeReplaysCount([FromBody] ArcadeReplaysRequest request, CancellationToken token = default)
    {
        var count = await replayRepository.GetArcadeReplaysCount(request, token);
        return Ok(count);
    }
}
