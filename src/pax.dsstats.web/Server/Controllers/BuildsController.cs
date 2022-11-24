using Microsoft.AspNetCore.Mvc;
using pax.dsstats.dbng.Services;
using pax.dsstats.shared;

namespace pax.dsstats.web.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BuildsController : Controller
{
    private readonly BuildService buildService;

    public BuildsController(BuildService buildService)
    {
        this.buildService = buildService;
    }

    [HttpGet("topplayers/{std}/{min}")]
    public List<RequestNames> GetTopPlayers(bool std, int min)
    {
        return buildService.GetTopPlayers(std, min);
    }

    [HttpPost]
    [Route("GetBuild")]
    public async Task<BuildResponse> GetBuild(BuildRequest request)
    {
        return await buildService.GetBuild(request);
    }
}
