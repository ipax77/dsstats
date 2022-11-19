using Microsoft.AspNetCore.Mvc;
using pax.dsstats.shared;

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
        return await Task.FromResult(100);
    }

    [HttpPost]
    [Route("GetRatings")]
    public async Task<ActionResult<PlayerRatingsResult>> GetRatings(RatingsRequest request, CancellationToken token = default)
    {
        try
        {
            return await ratingRepository.GetRatings(request, token);
        }
        catch (OperationCanceledException) { }
        return new NoContentResult();
    }

    [HttpGet]
    [Route("GetPlayerRatings/{toonId}")]
    public async Task<ActionResult<string>> GetPlayerRatings(int toonId, CancellationToken token = default)
    {
        var rating = await ratingRepository.GetPlayerRatings(toonId, token);
        if (rating == null)
        {
            return new NotFoundResult();
        }
        return rating;
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
        return await ratingRepository.GetRatingsDeviation();
    }

    [HttpGet]
    [Route("PlayerRating/{toonId}")]
    public async Task<PlayerRating?> GetPlayerRating(int toonId)
    {
        return await ratingRepository.GetPlayerRating(toonId);
    }
}
