using Microsoft.AspNetCore.Mvc;
using pax.dsstats.dbng.Repositories;
using pax.dsstats.dbng.Services;
using pax.dsstats.shared;

namespace pax.dsstats.web.Server.Controllers.v2
{
    [ApiController]
    [Route("api/v2/[controller]")]
    public class StatsController : ControllerBase
    {
        private readonly IReplayRepository replayRepository;
        private readonly BuildService buildService;
        private readonly IStatsService statsService;
        private readonly CmdrsService cmdrService;

        public StatsController(IReplayRepository replayRepository,
                               BuildService buildService,
                               IStatsService statsService,
                               CmdrsService cmdrService)
        {
            this.replayRepository = replayRepository;
            this.buildService = buildService;
            this.statsService = statsService;
            this.cmdrService = cmdrService;
        }

        [HttpGet]
        [Route("GetDetailReplay/{replayHash}")]
        public async Task<ActionResult<ReplayDetailsDto?>> GetDetailReplay(string replayHash, CancellationToken token = default)
        {
            var replayDto = await replayRepository.GetDetailReplay(replayHash, false, token);
            if (replayDto == null)
            {
                return NotFound();
            }
            return replayDto;
        }

        [HttpGet]
        [Route("GetReplay/{replayHash}")]
        public async Task<ActionResult<ReplayDto?>> GetReplay(string replayHash, CancellationToken token = default)
        {
            var replayDto = await replayRepository.GetReplay(replayHash, false, token);
            if (replayDto == null)
            {
                return NotFound();
            }
            return replayDto;
        }

        [HttpPost]
        [Route("GetReplaysCount")]
        public async Task<ActionResult<int>> GetReplaysCount(ReplaysRequest request, CancellationToken token = default)
        {
            try
            {
                return await replayRepository.GetReplaysCount(request, token);
            }
            catch (OperationCanceledException) { }
            return NoContent();
        }

        [HttpPost]
        [Route("GetReplays")]
        public async Task<ActionResult<ICollection<ReplayListDto>>> GetReplays(ReplaysRequest request, CancellationToken token = default)
        {
            try
            {
                return Ok(await replayRepository.GetReplays(request, token));
            }
            catch (OperationCanceledException)
            {
                return NoContent();
            }
        }

        [HttpPost]
        [Route("GetEventReplaysCount")]
        public async Task<ActionResult<int>> GetEventReplaysCount(ReplaysRequest request, CancellationToken token = default)
        {
            try
            {
                return await replayRepository.GetEventReplaysCount(request, token);
            }
            catch (OperationCanceledException) { }
            return NoContent();
        }

        [HttpPost]
        [Route("GetEventReplays")]
        public async Task<ActionResult<ICollection<ReplayListEventDto>>> GetEventReplays(ReplaysRequest request, CancellationToken token = default)
        {
            try
            {
                return Ok(await replayRepository.GetEventReplays(request, token));
            }
            catch (OperationCanceledException)
            {
                return NoContent();
            }
        }

        [HttpGet]
        [Route("GetTournaments")]
        public async Task<List<EventListDto>> GetTournaments()
        {
            return await replayRepository.GetTournaments();
        }

        [HttpPost]
        [Route("GetStats")]
        public async Task<StatsResponse> GetStats(StatsRequest request, CancellationToken token = default)
        {
            return await statsService.GetStatsResponse(request);
        }

        [HttpPost]
        [Route("GetTourneyStats")]
        public async Task<StatsResponse> GetTourneyStats(StatsRequest request, CancellationToken token = default)
        {
            return await statsService.GetTourneyStats(request, token);
        }

        [HttpPost]
        [Route("GetBuild")]
        public async Task<BuildResponse> GetBuild(BuildRequest request)
        {
            return await buildService.GetBuild(request);
        }

        [HttpGet]
        [Route("GetPlayerDetails/{toonId}")]
        public async Task<PlayerDetailDto> GetPlayerDetails(int toonId)
        {
            return await statsService.GetPlayerDetails(toonId);
        }

        [HttpGet]
        [Route("GetPlayerDetailsNg/{toonId}/{rating}")]
        public async Task<ActionResult<PlayerDetailsResult>> GetPlayerDetailsNg(int toonId, int rating, CancellationToken token)
        {
            try
            {
                return await statsService.GetPlayerDetails(toonId, (RatingType)rating, token);
            }
            catch (OperationCanceledException) { }
            return NoContent();
        }

        [HttpGet]
        [Route("GetPlayerGroupDetails/{toonId}/{rating}")]
        public async Task<ActionResult<PlayerDetailsGroupResult>> GetPlayerGroupDetails(int toonId, int rating, CancellationToken token)
        {
            try
            {
                return await statsService.GetPlayerGroupDetails(toonId, (RatingType)rating, token);
            }
            catch (OperationCanceledException) { }
            return NoContent();
        }

        [HttpGet]
        [Route("GetPlayerMatchups/{toonId}/{rating}")]
        public async Task<ActionResult<List<PlayerMatchupInfo>>> GetPlayerMatchups(int toonId, int rating, CancellationToken token)
        {
            try
            {
                return await statsService.GetPlayerMatchups(toonId, (RatingType)rating, token);
            }
            catch (OperationCanceledException) { }
            return NoContent();
        }

        [HttpPost]
        [Route("GetCmdrInfo")]
        public async Task<ActionResult<CmdrResult>> GetCmdrInfo(CmdrRequest cmdrRequest, CancellationToken token = default)
        {
            try
            {
                return await cmdrService.GetCmdrInfo(cmdrRequest, token);
            }
            catch (OperationCanceledException) { }
            return NoContent();
        }

        [HttpPost]
        [Route("GetCrosstable")]
        public async Task<ActionResult<CrossTableResponse>> GetCrosstable(CrossTableRequest request, CancellationToken token = default)
        {
            try
            {
                return await statsService.GetCrossTable(request, token);
            }
            catch (OperationCanceledException) { }
            return NoContent();
        }

        [HttpPost]
        [Route("GetTeamReplays")]
        public async Task<ActionResult<List<BuildResponseReplay>>> GetTeamReplays(CrossTableReplaysRequest request, CancellationToken token)
        {
            try
            {
                return await statsService.GetTeamReplays(request, token);
            }
            catch (OperationCanceledException) { }
            return NoContent();
        }

        [HttpPost]
        [Route("GetStatsUpgrades")]
        public async Task<ActionResult<StatsUpgradesResponse>> GetUpgradeStats(BuildRequest buildRequest, CancellationToken token)
        {
            try
            {
                return await statsService.GetUpgradeStats(buildRequest, token);
            }
            catch (OperationCanceledException) { }
            return NoContent();
        }

        [HttpPost]
        [Route("GetGameInfo")]
        public async Task<ActionResult<GameInfoResult>> GetGameInfo(GameInfoRequest request, CancellationToken token)
        {
            try
            {
                return await statsService.GetGameInfo(request, token);
            }
            catch (OperationCanceledException) { }
            return NoContent();
        }

        [HttpGet]
        [Route("GetServerStats")]
        public async Task<ActionResult<ServerStatsResponse>> GetServerStats(CancellationToken token)
        {
            return await statsService.GetServerStats(token);
        }
    }
}