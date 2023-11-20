using dsstats.shared;
using dsstats.shared.Interfaces;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace dsstats.api.Controllers;

[EnableCors("dsstatsOrigin")]
[ApiController]
[Route("api/v1/[controller]")]
public class StatsController : Controller
{
    private readonly IWinrateService winrateService;
    private readonly ITimelineService timelineService;
    private readonly ISynergyService synergyService;
    private readonly IDurationService durationService;
    private readonly IDamageService damageService;
    private readonly ICountService countService;
    private readonly ITeamcompService teamcompService;

    public StatsController(IWinrateService winrateService,
                           ITimelineService timelineService,
                           ISynergyService synergyService,
                           IDurationService durationService,
                           IDamageService damageService,
                           ICountService countService,
                           ITeamcompService teamcompService)
    {
        this.winrateService = winrateService;
        this.timelineService = timelineService;
        this.synergyService = synergyService;
        this.durationService = durationService;
        this.damageService = damageService;
        this.countService = countService;
        this.teamcompService = teamcompService;
    }

    [HttpPost]
    [Route("winrate")]
    public async Task<WinrateResponse> GetWinrate(WinrateRequest request, CancellationToken token = default)
    {
        return await winrateService.GetWinrate(request, token);
    }

    [HttpPost]
    [Route("timeline")]
    public async Task<TimelineResponse> GetTimeline(StatsRequest request, CancellationToken token = default)
    {
        return await timelineService.GetTimeline(request, token);
    }

    [HttpGet]
    [Route("dsupdates/{timePeriod:int}")]
    public async Task<List<DsUpdateInfo>> GetDsUpdates(int timePeriod, CancellationToken token = default)
    {
        return await timelineService.GetDsUpdates((TimePeriod)timePeriod, token);
    }

    [HttpPost]
    [Route("synergy")]
    public async Task<SynergyResponse> GetSynergy(StatsRequest request, CancellationToken token = default)
    {
        return await synergyService.GetSynergy(request, token);
    }

    [HttpPost]
    [Route("duration")]
    public async Task<DurationResponse> GetDuration(StatsRequest request, CancellationToken token = default)
    {
        return await durationService.GetDuration(request, token);
    }

    [HttpPost]
    [Route("damage")]
    public async Task<DamageResponse> GetDamage(StatsRequest request, CancellationToken token)
    {
        return await damageService.GetDamage(request, token);
    }

    [HttpPost]
    [Route("count")]
    public async Task<CountResponse> GetCount(StatsRequest request, CancellationToken token)
    {
        return await countService.GetCount(request, token);
    }

    [HttpPost]
    [Route("teamcomp")]
    public async Task<TeamcompResponse> GetTeamcompResult(TeamcompRequest request, CancellationToken token)
    {
        return await teamcompService.GetTeamcompResult(request, token);
    }

    [HttpPost]
    [Route("teamcompreplayscount")]
    public async Task<int> GetTeamcompReplaysCount(TeamcompReplaysRequest request, CancellationToken token = default)
    {
        return await teamcompService.GetReplaysCount(request, token);
    }

    [HttpPost]
    [Route("teamcompreplays")]
    public async Task<List<ReplayListDto>> GetTeamcompReplays(TeamcompReplaysRequest request, CancellationToken token)
    {
        return await teamcompService.GetReplays(request, token);
    }
}
