using Microsoft.AspNetCore.Mvc;
using pax.dsstats.dbng.Repositories;
using pax.dsstats.dbng.Services;
using pax.dsstats.shared;

namespace pax.dsstats.web.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
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
        [Route("GetStats")]
        public async Task<StatsResponse> GetStats(StatsRequest request, CancellationToken token = default)
        {
            return await statsService.GetStatsResponse(request);
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
    }

}
