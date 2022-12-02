using pax.dsstats.dbng.Repositories;
using pax.dsstats.dbng.Services;
using pax.dsstats.shared;
using pax.dsstats.shared.Raven;

namespace sc2dsstats.maui.Services;

public class DataService : IDataService
{
    private readonly IReplayRepository replayRepository;
    private readonly BuildService buildService;
    private readonly IStatsService statsService;
    private readonly IRatingRepository ratingRepository;

    public DataService(IReplayRepository replayRepository,
                       BuildService buildService,
                       IStatsService statsService,
                       IRatingRepository ratingRepository)
    {
        this.replayRepository = replayRepository;
        this.buildService = buildService;
        this.statsService = statsService;
        this.ratingRepository = ratingRepository;
    }

    public async Task<ReplayDto?> GetReplay(string replayHash, CancellationToken token = default)
    {
        var replayDto = await replayRepository.GetReplay(replayHash, true, token);
        if (replayDto == null)
        {
            return null;
        }
        return replayDto;
    }

    public async Task<int> GetReplaysCount(ReplaysRequest request, CancellationToken token = default)
    {
        return await replayRepository.GetReplaysCount(request, token);
    }

    public async Task<ICollection<ReplayListDto>> GetReplays(ReplaysRequest request, CancellationToken token = default)
    {
        return await replayRepository.GetReplays(request, token);
    }

    public async Task<ICollection<string>> GetReplayPaths()
    {
        return await replayRepository.GetReplayPaths();
    }

    public async Task<List<string>> GetTournaments()
    {
        return await replayRepository.GetTournaments();
    }

    public async Task<StatsResponse> GetStats(StatsRequest request, CancellationToken token = default)
    {
        return await statsService.GetStatsResponse(request);
    }

    public async Task<BuildResponse> GetBuild(BuildRequest request, CancellationToken token = default)
    {
        return await buildService.GetBuild(request, token);
    }

    public async Task<int> GetRatingsCount(RatingsRequest request, CancellationToken token = default)
    {
        return await ratingRepository.GetRatingsCount(request, token);
    }

    public async Task<RatingsResult> GetRatings(RatingsRequest request, CancellationToken token = default)
    {
        return await ratingRepository.GetRatings(request, token);
    }

    public async Task<List<MmrDevDto>> GetRatingsDeviation()
    {
        return await ratingRepository.GetRatingsDeviation();
    }

    public async Task<PlayerDetailDto> GetPlayerDetails(int toonId, CancellationToken token)
    {
        return await statsService.GetPlayerDetails(toonId, token);
    }

    public async Task<List<MmrDevDto>> GetRatingsDeviationStd()
    {
        return await ratingRepository.GetRatingsDeviationStd();
    }

    public async Task<List<RequestNames>> GetTopPlayers(bool std)
    {
        return await buildService.GetTopPlayers(std, 25);
    }

    public async Task<CmdrResult> GetCmdrInfo(CmdrRequest request, CancellationToken token = default)
    {
        return await Task.FromResult(new CmdrResult());
    }

    public async Task<CrossTableResponse> GetCrossTable(CrossTableRequest request, CancellationToken token = default)
    {
        return await Task.FromResult(new CrossTableResponse());
    }
}
