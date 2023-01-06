using Microsoft.AspNetCore.Mvc;
using pax.dsstats.shared;
using pax.dsstats;

namespace pax.dsstats.web.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RatingsController
{
    private readonly IRatingRepository ratingRepository;

    public RatingsController(IRatingRepository ratingRepository)
    {
        this.ratingRepository = ratingRepository;
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
}
