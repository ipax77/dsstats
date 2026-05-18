using dsstats.shared;
using dsstats.shared.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace dsstats.api.Controllers;

[ApiController]
[Route("api10/[controller]")]
public sealed class BuildDetailsController(IBuildDetailsService buildDetailsService) : Controller
{
    [HttpPost("overview")]
    public async Task<ActionResult<List<BuildDetailsOverviewRow>>> GetOverview(
        [FromBody] BuildDetailsRequest request,
        CancellationToken token = default)
    {
        var rows = await buildDetailsService.GetOverview(request, token);
        return Ok(rows);
    }

    [HttpPost("matchups")]
    public async Task<ActionResult<List<BuildDetailsMatchupRow>>> GetMatchups(
        [FromBody] BuildDetailsMatchupRequest request,
        CancellationToken token = default)
    {
        var rows = await buildDetailsService.GetMatchups(request, token);
        return Ok(rows);
    }

    [HttpPost("samples")]
    public async Task<ActionResult<List<BuildDetailsSampleReplay>>> GetSampleReplays(
        [FromBody] BuildDetailsSamplesRequest request,
        CancellationToken token = default)
    {
        var rows = await buildDetailsService.GetSampleReplays(request, token);
        return Ok(rows);
    }

    [HttpPost("team-builds/overview")]
    public async Task<ActionResult<List<BuildDetailsTeamBuildOverviewRow>>> GetTeamBuildOverview(
        [FromBody] BuildDetailsRequest request,
        CancellationToken token = default)
    {
        var rows = await buildDetailsService.GetTeamBuildOverview(request, token);
        return Ok(rows);
    }

    [HttpPost("team-builds/samples")]
    public async Task<ActionResult<List<BuildDetailsTeamBuildSampleReplay>>> GetTeamBuildSampleReplays(
        [FromBody] BuildDetailsTeamBuildSamplesRequest request,
        CancellationToken token = default)
    {
        var rows = await buildDetailsService.GetTeamBuildSampleReplays(request, token);
        return Ok(rows);
    }
}
