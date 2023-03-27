using dsstats.ratings.api.Services;
using Microsoft.AspNetCore.Mvc;

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
}
