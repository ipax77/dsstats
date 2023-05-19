using Microsoft.AspNetCore.Mvc;
using pax.dsstats.dbng;
using pax.dsstats.dbng.Services;
using pax.dsstats.shared;
using System.ComponentModel.DataAnnotations.Schema;

namespace pax.dsstats.web.Server.Controllers.v1;

[ApiController]
[Route("api/v1/[controller]")]
public class RatingsController
{
    private readonly IRatingRepository ratingRepository;
    private readonly PlayerService playerService;

    public RatingsController(IRatingRepository ratingRepository, PlayerService playerService)
    {
        this.ratingRepository = ratingRepository;
        this.playerService = playerService;
    }

    [HttpPost]
    [Route("GetRatingsCount")]
    public async Task<ActionResult<int>> GetRatingsCount(RatingsRequest request, CancellationToken token = default)
    {
        try
        {
            return await ratingRepository.GetRatingsCount(request, token);
        }
        catch (OperationCanceledException) { }
        return new NoContentResult();
    }

    [HttpPost]
    [Route("GetRatings")]
    public async Task<ActionResult<RatingsResult>> GetRatings(RatingsRequest request, CancellationToken token = default)
    {
        try
        {
            return await ratingRepository.GetRatings(request, token);
        }
        catch (OperationCanceledException) { }
        return new NoContentResult();
    }

    [HttpGet]
    [Route("GetRatingsDeviation")]
    public async Task<List<MmrDevDto>> GetRatingsDeviation()
    {
        return await ratingRepository.GetRatingsDeviation();
    }

    [HttpGet]
    [Route("GetRatingsDeviationStd")]
    public async Task<List<MmrDevDto>> GetRatingsDeviationStd()
    {
        return await ratingRepository.GetRatingsDeviationStd();
    }

    [HttpGet]
    [Route("PlayerRating/{toonId}")]
    public async Task<RavenPlayerDetailsDto> GetPlayerRating(int toonId)
    {
        return await ratingRepository.GetPlayerDetails(toonId);
    }

    [HttpPost]
    [Route("GetToonIdRatings")]
    public async Task<ToonIdRatingResponse> GetToonIdRatings(ToonIdRatingRequest request, CancellationToken token)
    {
        return await ratingRepository.GetToonIdRatings(request, token);
    }

    [HttpPost]
    [Route("GetToonIdCalcRatings")]
    public async Task<List<PlayerRatingReplayCalcDto>> GetToonIdCalcRatings(ToonIdRatingRequest request, CancellationToken token)
    {
        return await ratingRepository.GetToonIdCalcRatings(request, token);
    }

    [HttpPost]
    [Route("GetPlayerIdRatings")]
    public async Task<ToonIdRatingResponse> GetPlayerIdRatings(PlayerIdRatingRequest request, CancellationToken token)
    {
        return await ratingRepository.GetPlayerIdRatings(request, token);
    }

    [HttpPost]
    [Route("GetPlayerIdCalcRatings")]
    public async Task<List<PlayerRatingReplayCalcDto>> GetPlayerIdCalcRatings(PlayerIdRatingRequest request, CancellationToken token)
    {
        return await ratingRepository.GetPlayerIdCalcRatings(request, token);
    }

    //[HttpPost]
    //[Route("GetRatingChangesCount")]
    //public async Task<int> GetRatingChangesCount(RatingChangesRequest request, CancellationToken token)
    //{
    //    return await ratingRepository.GetRatingChangesCount(request, token);
    //}

    //[HttpPost]
    //[Route("GetRatingChanges")]
    //public async Task<RatingChangesResult> GetRatingChanges(RatingChangesRequest request, CancellationToken token)
    //{
    //    return await ratingRepository.GetRatingChanges(request, token);
    //}

    [HttpPost]
    [Route("GetDistribution")]
    public async Task<DistributionResponse> GetDistribution(DistributionRequest request, CancellationToken token)
    {
        return await ratingRepository.GetDistribution(request, token);
    }

    [HttpPost]
    [Route("GetPlayerDetails")]
    public async Task<PlayerDetailResponse> GetPlayerDetails(PlayerDetailRequest request, CancellationToken token)
    {
        return await playerService.GetPlayerDetails(request, token);
    }

    [HttpGet]
    [Route("GetPlayerDatailSummary/{toonId}")]
    public async Task<PlayerDetailSummary> GetPlayerSummary(int toonId, CancellationToken token = default)
    {
        return await playerService.GetPlayerSummary(toonId, token);
    }

    [HttpGet]
    [Route("GetPlayerRatingDetails/{toonId}/{ratingType}")]
    public async Task<PlayerRatingDetails> GetPlayerRatingDetails(int toonId, int ratingType, CancellationToken token = default)
    {
        return await playerService.GetPlayerRatingDetails(toonId, (RatingType)ratingType, token);
    }

    [HttpGet]
    [Route("GetPlayerCmdrAvgGain/{toonId}/{ratingType}/{timePeriod}")]
    public async Task<List<PlayerCmdrAvgGain>> GetPlayerCmdrAvgGain(int toonId, int ratingType, int timePeriod, CancellationToken token)
    {
        return await playerService.GetPlayerCmdrAvgGain(toonId, (RatingType)ratingType, (TimePeriod)timePeriod, token);
    }

    [HttpPost]
    [Route("GetIdPlayerDetails")]
    public async Task<PlayerDetailResponse> GetIdPlayerDetails(PlayerDetailRequest request, CancellationToken token)
    {
        return await playerService.GetPlayerIdPlayerDetails(request, token);
    }

    [HttpGet]
    [Route("GetIdPlayerDatailSummary/{toonId}/{regionId}/{realmId}")]
    public async Task<PlayerDetailSummary> GetIdPlayerSummary(int toonId, int regionId, int realmId, CancellationToken token = default)
    {
        return await playerService.GetPlayerPlayerIdSummary(new(toonId, realmId, regionId), token);
    }

    [HttpGet]
    [Route("GetIdPlayerRatingDetails/{toonId}/{regionId}/{realmId}/{ratingType}")]
    public async Task<PlayerRatingDetails> GetIdPlayerRatingDetails(int toonId, int regionId, int realmId, int ratingType, CancellationToken token = default)
    {
        return await playerService.GetPlayerIdPlayerRatingDetails(new(toonId, realmId, regionId), (RatingType)ratingType, token);
    }

    [HttpGet]
    [Route("GetIdPlayerCmdrAvgGain/{toonId}/{regionId}/{realmId}/{ratingType}/{timePeriod}")]
    public async Task<List<PlayerCmdrAvgGain>> GetIdPlayerCmdrAvgGain(int toonId, int regionId, int realmId, int ratingType, int timePeriod, CancellationToken token)
    {
        return await playerService.GetPlayerIdPlayerCmdrAvgGain(new(toonId, realmId, regionId), (RatingType)ratingType, (TimePeriod)timePeriod, token);
    }
}

