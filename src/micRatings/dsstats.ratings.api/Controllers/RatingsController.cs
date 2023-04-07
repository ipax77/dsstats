using dsstats.ratings.api.Services;
using Microsoft.AspNetCore.Mvc;
using pax.dsstats.shared;

namespace dsstats.ratings.api.Controllers;

[ApiController]
[Route("/api/v1/[controller]")]
[ServiceFilter(typeof(AuthenticationFilterAttribute))]
public class RatingsController
{
    private readonly RatingsService ratingsService;

    public RatingsController(RatingsService ratingsService)
    {
        this.ratingsService = ratingsService;
    }

    [HttpGet]
    public async Task<ActionResult> ProduceRatings()
    {
        await ratingsService.ProduceRatings();
        return new OkResult();
    }

    [HttpGet]
    [Route("reports")]
    public ActionResult<List<RatingsReport>> GetRatingResults()
    {
        return ratingsService.GetResults();
    }
}
