using Microsoft.Extensions.Logging;
using pax.dsstats.dbng.Repositories;
using pax.dsstats.dbng.Services;
using pax.dsstats.shared;

namespace sc2dsstats.maui.Services;

public partial class DataService : IDataService
{
    private readonly IReplayRepository replayRepository;
    private readonly BuildService buildService;
    private readonly IStatsService statsService;
    private readonly IRatingRepository ratingRepository;
    private readonly ILogger<DataService> logger;
    private readonly HttpClient httpClient;

    public DataService(IReplayRepository replayRepository,
                       BuildService buildService,
                       IStatsService statsService,
                       IRatingRepository ratingRepository,
                       ILogger<DataService> logger)
    {
        this.replayRepository = replayRepository;
        this.buildService = buildService;
        this.statsService = statsService;
        this.ratingRepository = ratingRepository;
        this.logger = logger;
        httpClient = GetHttpClient();
    }

    public async Task<ReplayDto?> GetReplay(string replayHash, CancellationToken token = default)
    {
        var replayDto = await replayRepository.GetReplay(replayHash, true, token);
        if (replayDto == null)
        {
            return await GetReplayFromServer(replayHash, token);            
        }
        return replayDto;
    }

    public async Task<int> GetReplaysCount(ReplaysRequest request, CancellationToken token = default)
    {
        if (request.FromServer)
        {
            return await GetReplaysCountFromServer(request, token);
        }
        else
        {
            return await replayRepository.GetReplaysCount(request, token);
        }
    }

    public async Task<ICollection<ReplayListDto>> GetReplays(ReplaysRequest request, CancellationToken token = default)
    {
        if (request.FromServer)
        {
            return await GetReplaysFromServer(request, token);
        }
        else
        {
            return await replayRepository.GetReplays(request, token);
        }
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
        var details = await statsService.GetPlayerDetails(toonId, token);
        if (details.PlayerDetails.ToonId == 0)
        {
            return await GetPlayerDetailsFromServer(toonId, token);
        }
        return details;
    }

    public async Task<List<MmrDevDto>> GetRatingsDeviationStd()
    {
        return await ratingRepository.GetRatingsDeviationStd();
    }

    public async Task<List<RequestNames>> GetTopPlayers(bool std)
    {
        return await Task.FromResult(buildService.GetTopPlayers(std, 100));
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
