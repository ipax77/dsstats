using dsstats.shared;
using dsstats.shared.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace dsstats.api.Controllers;

[ApiController]
[Route("api8/v1/[controller]")]
public class TourneysController(ITourneysService tourneysService) : Controller
{
    [HttpGet]
    public async Task<ActionResult<List<TourneyDto>>> GetTourneys()
    {
        return await tourneysService.GetTourneys();
    }

    [HttpGet]
    [Route("groups")]
    public async Task<ActionResult<List<GroupStateDto>>> GetOpenGroups()
    {
        return await tourneysService.GetGroupStates();
    }

    [HttpPost]
    [Route("stats")]
    public async Task<ActionResult<TourneysStatsResponse>> GetTourneySats(TourneysStatsRequest request, CancellationToken token = default)
    {
        return await tourneysService.GetTourneyStats(request, token);
    }

    [HttpPost]
    [Route("replayscount")]
    public async Task<ActionResult<int>> GetReplaysCount(TourneysReplaysRequest request, CancellationToken token = default)
    {
        return await tourneysService.GetReplaysCount(request, token);
    }

    [HttpPost]
    [Route("replays")]
    public async Task<ActionResult<List<TourneysReplayListDto>>> GetReplays(TourneysReplaysRequest request, CancellationToken token)
    {
        return await tourneysService.GetReplays(request, token);
    }

    [HttpGet]
    [Route("download/{replayHash}")]
    public async Task<IActionResult> DownloadReplay(string replayHash)
    {
        var tuple = await tourneysService.DownloadReplay(replayHash);
        if (tuple is null)
        {
            return NotFound();
        }

        var filePath = tuple.Value.Item1;
        var fileName = tuple.Value.Item2;

        var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);

        try
        {
            return File(stream, "application/octet-stream", fileName);
        }
        catch
        {
            stream.Dispose();
            throw;
        }
    }

    [HttpGet]
    [Route("ihsessionscount")]
    public async Task<int> GetIhSessionsCount(CancellationToken token = default)
    {
        return await tourneysService.GetIhSessionsCount(token);
    }

    [HttpGet]
    [Route("ihsessions/{skip:int}/{take:int}")]
    public async Task<List<IhSessionListDto>> GetIhSessions(int skip, int take, CancellationToken token)
    {
        return await tourneysService.GetIhSessions(skip, take, token);
    }

    [HttpGet]
    [Route("ihsession/{groupId:guid}")]
    public async Task<IhSessionDto?> GetIhSession(Guid groupId)
    {
        return await tourneysService.GetIhSession(groupId);
    }

    [HttpGet]
    [Route("ihsessionreplays/{groupId:guid}")]
    public async Task<List<ReplayListDto>> GetReplays(Guid groupId)
    {
        return await tourneysService.GetReplays(groupId);
    }
}
