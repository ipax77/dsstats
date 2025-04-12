using dsstats.shared;
using dsstats.shared8;
using dsstats.shared8.Interfaces;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace dsstats.api.Controllers;

[EnableCors("dsstatsOrigin")]
[ApiController]
[Route("api/v1/[controller]")]
public class PlayerStatsController(IPlayerService playerService) : Controller
{
    [HttpGet("stats/{toonId:int}/{regionId:int}/{realmId:int}/{ratingType}")]
    public async Task<ActionResult<PlayerStatsResponse>> GetPlayerStats(int toonId,
                                                                        int regionId,
                                                                        int realmId,
                                                                        RatingNgType ratingType,
                                                                        CancellationToken token)
    {
        var result = await playerService.GetPlayerStats(new(toonId, realmId, regionId),
                                                        ratingType,
                                                        token);
        return Ok(result);
    }

    [HttpGet("avggain/{toonId:int}/{regionId:int}/{realmId:int}/{ratingType}/{timePeriod}")]
    public async Task<ActionResult<List<PlayerCmdrAvgGain>>> GetPlayerIdPlayerCmdrAvgGain(int toonId,
                                                                                          int regionId,
                                                                                          int realmId,
                                                                                          RatingNgType ratingType,
                                                                                          TimePeriod timePeriod,
                                                                                          CancellationToken token)
    {
        var result = await playerService.GetPlayerIdPlayerCmdrAvgGain(new(toonId, realmId, regionId),
                                                                      ratingType,
                                                                      timePeriod,
                                                                      token);
        return Ok(result);
    }
}
