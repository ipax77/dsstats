using Microsoft.AspNetCore.Mvc;
using pax.dsstats.shared;
using pax.dsstats.shared.Raven;

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
    public async Task<List<RavenPlayerDto>> GetPlayerRating(int toonId)
    {
        return await ratingRepository.GetPlayerRating(toonId);
    }
}
