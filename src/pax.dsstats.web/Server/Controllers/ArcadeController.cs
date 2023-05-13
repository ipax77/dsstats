using Microsoft.AspNetCore.Mvc;
using pax.dsstats.dbng.Services;
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

    [HttpPost]
    [Route("replayscount")]
    public async Task<ActionResult<int>> GetReplayCount(ArcadeReplaysRequest request, CancellationToken token)
    {
        return await arcadeService.GetReplayCount(request, token);
    }

    [HttpPost]
    [Route("replays")]
    public async Task<ActionResult<List<ArcadeReplayListDto>>> GetReplays(ArcadeReplaysRequest request, CancellationToken token)
    {
        return await arcadeService.GetArcadeReplays(request, token);
    }

    [HttpGet]
    [Route("replay/{id:int}")]
    public async Task<ArcadeReplayDto?> GetArcadeReplay(int id, CancellationToken token = default)
    {
        return await arcadeService.GetArcadeReplay(id, token);
    }

    [HttpPost]
    [Route("playerdetails")]
    public async Task<ArcadePlayerDetails> GetPlayerDetails(ArcadePlayerId playerId, CancellationToken token)
    {
        return await arcadeService.GetPlayerDetails(playerId, token);
    }

    [HttpGet]
    [Route("playerdetails/{arcadePlayerId:int}")]
    public async Task<ArcadePlayerDetails> GetPlayerDetails(int arcadePlayerId, CancellationToken token)
    {
        return await arcadeService.GetPlayerDetails(arcadePlayerId, token);
    }

    [HttpPost]
    [Route("moreplayerdetails/{ratingType:int}")]
    public async Task<ArcadePlayerMoreDetails> GetMorePlayerDatails([FromBody] ArcadePlayerId playerId,
                                                                    int ratingType,
                                                                    CancellationToken token)
    {
        return await arcadeService.GetMorePlayerDatails(playerId, (RatingType)ratingType, token);
    }

    [HttpPost]
    [Route("playerratingchartdata/{ratingType:int}")]
    public async Task<ActionResult<List<ReplayPlayerChartDto>>> GetPlayerRatingChartData([FromBody] PlayerId playerId, int ratingType)
    {
        return await arcadeService.GetPlayerRatingChartData(playerId, (RatingType)ratingType);
    }

    [HttpGet]
    [Route("getrequestnames/{playerId:int}")]
    public async Task<ActionResult<RequestNames>> GetRequetNamesFromId(int playerId, CancellationToken token = default)
    {
        var requestNames = await arcadeService.GetRequetNamesFromId(playerId, token);
        if (requestNames == null)
        {
            return new NotFoundResult();
        }
        else
        {
            return requestNames;
        }
    }
}
