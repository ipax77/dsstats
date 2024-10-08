
using dsstats.shared;
using dsstats.shared.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace dsstats.api.Controllers;

[ApiController]
[Route("api8/v1/[controller]")]
public class PlayerController : Controller
{
    private readonly IPlayerService playerService;
    private readonly IReviewService reviewService;

    public PlayerController(IPlayerService playerService, IReviewService reviewService)
    {
        this.playerService = playerService;
        this.reviewService = reviewService;
    }

    [HttpGet]
    [Route("playeridname/{toonId:int}/{realmId:int}/{regionId:int}")]
    public async Task<string?> GetPlayerIdName(int toonId, int realmId, int regionId)
    {
        return await playerService.GetPlayerIdName(new(toonId, realmId, regionId));
    }

    [HttpPost]
    [Route("ratingscount")]
    public async Task<ActionResult<int>> GetRatingsCount(RatingsRequest request, CancellationToken token)
    {
        return await playerService.GetRatingsCount(request, token);
    }

    [HttpPost]
    [Route("ratings")]
    public async Task<ActionResult<List<ComboPlayerRatingDto>>> GetRatings(RatingsRequest request, CancellationToken token)
    {
        return await playerService.GetRatings(request, token);
    }

    [HttpGet]
    [Route("summary/{toonId:int}/{regionId:int}/{realmId:int}/{ratingType:int}/{ratingCalctype:int}")]
    public async Task<IActionResult> GetPlayerSummary(int toonId,
                                                      int regionId,
                                                      int realmId,
                                                      int ratingType,
                                                      int ratingCalctype,
                                                      CancellationToken token = default)
    {
        try
        {
            var summary = await playerService.GetPlayerPlayerIdSummary(new(toonId, realmId, regionId),
                                                                       (RatingType)ratingType,
                                                                       (RatingCalcType)ratingCalctype,
                                                                       token);
            return Ok(summary);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"An error occurred: {ex.Message}");
        }
    }

    [HttpGet]
    [Route("rating/{toonId:int}/{regionId:int}/{realmId:int}/{ratingType:int}/{ratingCalcType:int}")]
    public async Task<IActionResult> GetPlayerRatingDetails(int toonId,
                                                            int regionId,
                                                            int realmId,
                                                            int ratingType,
                                                            int ratingCalcType,
                                                            CancellationToken token = default)
    {
        try
        {
            var ratingDetails = await playerService.GetPlayerIdPlayerRatingDetails(new(toonId, realmId, regionId),
                                                                                   (RatingType)ratingType,
                                                                                   (RatingCalcType)ratingCalcType,
                                                                                   token);
            return Ok(ratingDetails);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"An error occurred: {ex.Message}");
        }
    }

    [HttpGet]
    [Route("cmdravggain/{toonId}/{regionId}/{realmId}/{ratingType}/{timePeriod}")]
    public async Task<IActionResult> GetPlayerCmdrAvgGain(int toonId, int regionId, int realmId, int ratingType, int timePeriod, CancellationToken token = default)
    {
        try
        {
            var cmdrAvgGain = await playerService.GetPlayerIdPlayerCmdrAvgGain(new(toonId, realmId, regionId), (RatingType)ratingType, (TimePeriod)timePeriod, token);
            return Ok(cmdrAvgGain);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"An error occurred: {ex.Message}");
        }
    }

    [HttpPost("details")]
    public async Task<IActionResult> GetPlayerDetails(PlayerDetailRequest request, CancellationToken token = default)
    {
        try
        {
            var playerDetails = await playerService.GetPlayerIdPlayerDetails(request, token);
            return Ok(playerDetails);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"An error occurred: {ex.Message}");
        }
    }

    [HttpPost]
    [Route("playerratingchartdata/{ratingType:int}")]
    public async Task<ActionResult<List<ReplayPlayerChartDto>>> GetPlayerRatingChartData(PlayerId playerId,
                                                                                         int ratingType,
                                                                                         CancellationToken token = default)
    {
        return await playerService.GetPlayerRatingChartData(playerId, (RatingType)ratingType, token);
    }

    [HttpPost]
    [Route("playerratingchartdata/{ratingType:int}/{ratingCalcType:int}")]
    public async Task<ActionResult<List<ReplayPlayerChartDto>>> GetPlayerRatingChartData(PlayerId playerId,
                                                                                     int ratingType,
                                                                                     int ratingCalcType,
                                                                                     CancellationToken token = default)
    {
        return await playerService.GetPlayerRatingChartData(playerId, (RatingCalcType)ratingCalcType, (RatingType)ratingType, token);
    }

    [HttpPost]
    [Route("playercommandersplayed/{ratingType:int}")]
    public async Task<ActionResult<List<CommanderInfo>>> GetPlayerIdCommandersPlayed(PlayerId playerId,
                                                                       int ratingType,
                                                                       CancellationToken token)
    {
        return await playerService.GetPlayerIdCommandersPlayed(playerId, (RatingType)ratingType, token);
    }

    [HttpPost]
    [Route("distribution")]
    public async Task<ActionResult<DistributionResponse>> GetDistribution(DistributionRequest request)
    {
        return await playerService.GetDistribution(request);
    }

    [HttpPost]
    [Route("review")]
    public async Task<ActionResult<ReviewResponse>> GetReview(ReviewRequest request, CancellationToken token)
    {
        return await reviewService.GetReview(request, token);
    }

    [HttpPost]
    [Route("reviewratingtypeinfo")]
    public async Task<ActionResult<ReviewResponse>> GetReviewRatingTypeInfo(ReviewRequest request, CancellationToken token)
    {
        return await reviewService.GetReviewRatingTypeInfo(request, token);
    }

    [HttpGet]
    [Route("reviewyear/{ratingType:int}/{year:int}")]
    public async Task<ActionResult<ReviewYearResponse>> GetYearReview(int ratingType, int year, CancellationToken token)
    {
        return await reviewService.GetYearReview((RatingType)ratingType, year, token);
    }

    [HttpGet]
    [Route("reviewyearratingtypeinfo/{ratingType:int}/{year:int}")]
    public async Task<ActionResult<ReviewYearResponse>> GetYearReviewratingtypeinfo(int ratingType, int year, CancellationToken token)
    {
        return await reviewService.GetYearRatingTypeReview((RatingType)ratingType, year, token);
    }
}
