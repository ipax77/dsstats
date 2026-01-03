using dsstats.shared;
using dsstats.shared.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace dsstats.api.Controllers;

[ApiController]
[Route("api10/[controller]")]
public class PlayersController : Controller
{
    private readonly IPlayerService _playerService;

    public PlayersController(IPlayerService playerService)
    {
        _playerService = playerService;
    }

    [HttpPost("ratings")]
    public async Task<IActionResult> GetRatings([FromBody] PlayerRatingsRequest request, CancellationToken token = default)
    {
        var ratings = await _playerService.GetRatings(request, token);
        return Ok(ratings);
    }

    [HttpPost("ratingscount")]
    public async Task<IActionResult> GetRatingsCount([FromBody] PlayerRatingsRequest request, CancellationToken token = default)
    {
        var count = await _playerService.GetRatingsCount(request, token);
        return Ok(count);
    }

    [EnableRateLimiting("fixed")]
    [HttpPost("stats")]
    public async Task<IActionResult> GetPlayerStats([FromBody] PlayerStatsRequest request, CancellationToken token = default)
    {
        var stats = await _playerService.GetPlayerStats(request, token);
        return Ok(stats);
    }

    [EnableRateLimiting("fixed")]
    [HttpPost("details")]
    public async Task<IActionResult> GetRatingDetails([FromBody] PlayerStatsRequest request, CancellationToken token = default)
    {
        var details = await _playerService.GetRatingDetails(request, token);
        return Ok(details);
    }

    [EnableRateLimiting("fixed")]
    [HttpPost("cmdrperf")]
    public async Task<IActionResult> GetCommandersPerformance([FromBody] PlayerStatsRequest request, CancellationToken token)
    {
        var perf = await _playerService.GetCommandersPerformance(request, token);
        return Ok(perf);
    }

    [EnableRateLimiting("fixed")]
    [HttpPost("cmdrstrength")]
    public async Task<IActionResult> GetCmdrPlayerInfos([FromBody] CmdrStrenghtRequest request, CancellationToken token = default)
    {
        var infos = await _playerService.GetCmdrPlayerInfos(request, token);
        return Ok(infos);
    }

    [HttpPost("ratingdistribution")]
    public async Task<IActionResult> GetRatingDistribution([FromBody] DistributionRequest request)
    {
        var distribution = await _playerService.GetRatingDistribution(request);
        return Ok(distribution);
    }
}
