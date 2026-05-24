using dsstats.shared;
using dsstats.shared.Interfaces;
using dsstats.dbServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace dsstats.api.Controllers;

[ApiController]
[Route("api10/[controller]")]
public class ReplaysController(
    IReplayRepository replayRepository,
    ReplayUserRatingService replayUserRatingService) : Controller
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
    [HttpGet("{replayHash}/spawn-playback")]
    public async Task<IActionResult> GetReplaySpawnPlayback(string replayHash, CancellationToken token)
    {
        var payload = await replayRepository.GetReplaySpawnPlayback(replayHash, token);
        if (payload is null || payload.Length == 0)
        {
            return NotFound();
        }

        return File(payload, "application/octet-stream");
    }

    [EnableRateLimiting("fixed")]
    [HttpGet("{replayHash}/spawn-positions")]
    public async Task<IActionResult> GetReplaySpawnPositions(string replayHash, CancellationToken token)
    {
        var positions = await replayRepository.GetReplaySpawnPositions(replayHash, token);
        if (positions is null || positions.Players.Count == 0)
        {
            return NotFound();
        }

        return Ok(positions);
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

    [EnableRateLimiting("fixed")]
    [HttpGet("{replayHash}/user-rating")]
    public async Task<IActionResult> GetReplayUserRating(string replayHash, CancellationToken token)
    {
        var ipHash = replayUserRatingService.GetIpHash(HttpContext.Connection.RemoteIpAddress);
        var rating = await replayUserRatingService.GetRatingAsync(replayHash, ipHash, token);
        if (rating == null)
        {
            return NotFound();
        }

        return Ok(rating);
    }

    [EnableRateLimiting("replay-user-rating")]
    [HttpPost("{replayHash}/user-rating")]
    public async Task<IActionResult> SubmitReplayUserRating(
        string replayHash,
        [FromBody] ReplayUserRatingRequest request,
        CancellationToken token)
    {
        var ipHash = replayUserRatingService.GetIpHash(HttpContext.Connection.RemoteIpAddress);
        var result = await replayUserRatingService.SubmitRatingAsync(replayHash, ipHash, request.Score, token);
        return result.Status switch
        {
            ReplayUserRatingSubmitStatus.Accepted => Ok(result.Rating),
            ReplayUserRatingSubmitStatus.InvalidScore => BadRequest(result.Rating),
            ReplayUserRatingSubmitStatus.ReplayNotFound => NotFound(),
            ReplayUserRatingSubmitStatus.CooldownActive => StatusCode(StatusCodes.Status429TooManyRequests, result.Rating),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
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
