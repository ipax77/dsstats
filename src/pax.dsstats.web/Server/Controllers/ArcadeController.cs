using Microsoft.AspNetCore.Mvc;
using pax.dsstats.shared;
using pax.dsstats.shared.Arcade;

namespace pax.dsstats.web.Server.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class ArcadeController
{
    private readonly IArcadeService arcadeService;

    public ArcadeController(IArcadeService arcadeService)
    {
        this.arcadeService = arcadeService;
    }

    [HttpPost]
    [Route("count")]
    public async Task<ActionResult<int>> GetCount(ArcadeRatingsRequest request, CancellationToken token)
    {
        return await arcadeService.GetRatingsCount(request, token);
    }

    [HttpPost]
    [Route("ratings")]
    public async Task<ActionResult<List<ArcadePlayerRatingDto>>> GetRatings(ArcadeRatingsRequest request, CancellationToken token)
    {
        return await arcadeService.GetRatings(request, token);
    }

    [HttpPost]
    [Route("distribution")]
    public async Task<ActionResult<DistributionResponse>> GetDistribution(DistributionRequest request, CancellationToken token = default)
    {
        return await arcadeService.GetDistribution(request, token);
    }
}
