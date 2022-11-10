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
        private readonly IStatsRepository statsRepository;
        private readonly BuildService buildService;
        private readonly IStatsService statsService;
        private readonly MmrService mmrService;

        public StatsController(IReplayRepository replayRepository,
                               IStatsRepository statsRepository,
                               BuildService buildService,
                               IStatsService statsService,
                               MmrService mmrService)
        {
            this.replayRepository = replayRepository;
            this.statsRepository = statsRepository;
            this.buildService = buildService;
            this.statsService = statsService;
            this.mmrService = mmrService;
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
        public async Task<int> GetReplaysCount(ReplaysRequest request, CancellationToken token = default)
        {
            return await replayRepository.GetReplaysCount(request, token);
        }

        [HttpPost]
        [Route("GetReplays")]
        public async Task<ICollection<ReplayListDto>> GetReplays(ReplaysRequest request, CancellationToken token = default)
        {
            return await replayRepository.GetReplays(request, token);
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
        public async Task<int> GetRatingsCount(RatingsRequest request, CancellationToken token = default)
        {
            // return await statsService.GetRatingsCount(request, token);
            return await mmrService.GetRatingsCount(request, token);
        }

        [HttpPost]
        [Route("GetRatings")]
        public async Task<List<PlayerRatingDto>> GetRatings(RatingsRequest request, CancellationToken token = default)
        {
            return await mmrService.GetRatings(request, token);
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
    }

}
