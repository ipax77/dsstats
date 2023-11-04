using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace dsstats.api.Controllers;

[EnableRateLimiting("fixed")]
[ApiController]
[Route("api/v1/[controller]")]
public class DsstatsController : Controller
{
    //private readonly IDsstatsService dsstatsService;

    //public DsstatsController(IDsstatsService dsstatsService)
    //{
    //    this.dsstatsService = dsstatsService;
    //}

    //[HttpGet]
    //[Route("replays")]
    //public async Task<ActionResult<DsstatsReplaysResponse>> GetReplays()
    //{
    //    return await dsstatsService.GetReplays(null);
    //}

    //[HttpGet]
    //[Route("replays/{page}")]
    //public async Task<ActionResult<DsstatsReplaysResponse>> GetReplays(string page)
    //{
    //    return await dsstatsService.GetReplays(page);
    //}
}
