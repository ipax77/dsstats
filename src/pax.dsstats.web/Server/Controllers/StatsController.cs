using Microsoft.AspNetCore.Mvc;
using pax.dsstats.dbng.Repositories;
using pax.dsstats.dbng.Services;
using pax.dsstats.shared;
using System.Net;

namespace pax.dsstats.web.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StatsController : ControllerBase
    {
        private readonly IReplayRepository replayRepository;
        private readonly IStatsRepository statsRepository;
        private readonly BuildService buildService;
        private readonly IStatsService statsService;
        private readonly MmrService mmrService;
        private readonly CmdrsService cmdrService;

        public StatsController(IReplayRepository replayRepository,
                               IStatsRepository statsRepository,
                               BuildService buildService,
                               IStatsService statsService,
                               MmrService mmrService,
                               CmdrsService cmdrService)
        {
            this.replayRepository = replayRepository;
            this.statsRepository = statsRepository;
            this.buildService = buildService;
            this.statsService = statsService;
            this.mmrService = mmrService;
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

        [HttpPost]
        [Route("GetRatingsCount")]
        public async Task<ActionResult<int>> GetRatingsCount(RatingsRequest request, CancellationToken token = default)
        {
            // return await statsService.GetRatingsCount(request, token);
            try
            {
                return await mmrService.GetRatingsCount(request, token);
            }
            catch (OperationCanceledException) { }
            return NoContent();
        }

        [HttpPost]
        [Route("GetRatings")]
        public async Task<ActionResult<List<PlayerRatingDto>>> GetRatings(RatingsRequest request, CancellationToken token = default)
        {
            try
            {
                return await mmrService.GetRatings(request, token);
            }
            catch (OperationCanceledException) { }
            return NoContent();
        }

        [HttpGet]
        [Route("GetPlayerRatings/{toonId}")]
        public async Task<ActionResult<string>> GetPlayerRatings(int toonId)
        {
            var rating = await mmrService.GetPlayerRatings(toonId);
            if (rating == null)
            {
                return NotFound();
            }
            return rating;
        }

        [HttpGet]
        [Route("GetRatingsDeviation")]
        public async Task<List<MmrDevDto>> GetRatingsDeviation()
        {
            return await mmrService.GetRatingsDeviation();
        }

        [HttpGet]
        [Route("GetRatingsDeviationStd")]
        public async Task<List<MmrDevDto>> GetRatingsDeviationStd()
        {
            return await mmrService.GetRatingsDeviationStd();
        }

        [HttpGet]
        [Route("GetPlayerDetails/{toonId}")]
        public async Task<ICollection<PlayerMatchupInfo>> GetPlayerDetailInfo(int toonId)
        {
            return await statsService.GetPlayerDetailInfo(toonId);
        }

        [HttpGet]
        [Route("PlayerRating/{toonId}")]
        public async Task<PlayerRatingDto?> GetPlayerRating(int toonId)
        {
            return await mmrService.GetPlayerRating(toonId);
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
    }

}
