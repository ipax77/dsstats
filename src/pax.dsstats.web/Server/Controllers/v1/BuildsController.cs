using Microsoft.AspNetCore.Mvc;
using pax.dsstats.dbng.Services;
using pax.dsstats.shared;

namespace pax.dsstats.web.Server.Controllers.v1;

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
    public async Task<List<RequestNames>> GetTopPlayers(bool std, int min)
    {
        return await buildService.GetTopPlayers(std, min);
    }

    [HttpPost]
    [Route("GetBuild")]
    public async Task<BuildResponse> GetBuild(BuildRequestV1 request)
    {
        return await buildService.GetBuild(request.GetBuildRequest());
    }
}

public record BuildRequestV1
{
    public List<RequestNames> PlayerNames { get; set; } = new();
    public Commander Interest { get; set; }
    public Commander Versus { get; set; }
    public string Timespan { get; set; } = "Patch 2.60";
    public DateTime StartTime { get; set; } = new DateTime(2020, 07, 28, 5, 23, 0);
    public DateTime EndTime { get; set; } = DateTime.Today;

    public BuildRequest GetBuildRequest()
    {
        return new BuildRequest
        {
            PlayerNames = PlayerNames,
            Interest = Interest,
            Versus = Versus,
            Timespan = Data.GetTimePeriodFromDeprecatedString(Timespan),
        };
    }
}