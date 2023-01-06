using Microsoft.Extensions.Logging;
using pax.dsstats.dbng.Repositories;
using pax.dsstats.dbng.Services;
using pax.dsstats.shared;
using System.Net.Http.Json;

namespace sc2dsstats.maui.Services;

public partial class DataService : IDataService
{
    private readonly IReplayRepository replayRepository;
    private readonly BuildService buildService;
    private readonly IStatsService statsService;
    private readonly IRatingRepository ratingRepository;
    private readonly IFromServerSwitchService fromServerSwitchService;
    private readonly ILogger<DataService> logger;
    private readonly HttpClient httpClient;

    public DataService(IReplayRepository replayRepository,
                       BuildService buildService,
                       IStatsService statsService,
                       IRatingRepository ratingRepository,
                       IFromServerSwitchService fromServerSwitchService,
                       ILogger<DataService> logger)
    {
        this.replayRepository = replayRepository;
        this.buildService = buildService;
        this.statsService = statsService;
        this.ratingRepository = ratingRepository;
        this.fromServerSwitchService = fromServerSwitchService;
        this.logger = logger;
        httpClient = new HttpClient();
        // httpClient.BaseAddress = new Uri("https://localhost:7174");
        httpClient.BaseAddress = new Uri("https://dsstats.pax77.org");
        httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
    }

    public void SetFromServer(bool fromServer)
    {
        fromServerSwitchService.SetFromServer(fromServer);
    }

    public bool GetFromServer()
    {
        return fromServerSwitchService.GetFromServer();
    }

    public async Task<ReplayDto?> GetReplay(string replayHash, CancellationToken token = default)
    {
        if (fromServerSwitchService.GetFromServer())
        {
            return await ServerGetReplay(replayHash, token);
        }
        else
        {
            var replayDto = await replayRepository.GetReplay(replayHash, true, token);
            if (replayDto == null)
            {
                return null;
            }
            return replayDto;
        }
    }

    public async Task<int> GetReplaysCount(ReplaysRequest request, CancellationToken token = default)
    {
        if (fromServerSwitchService.GetFromServer())
        {
            return await ServerGetReplaysCount(request, token);
        }
        else
        {
            return await replayRepository.GetReplaysCount(request, token);
        }
    }

    public async Task<ICollection<ReplayListDto>> GetReplays(ReplaysRequest request, CancellationToken token = default)
    {
        if (fromServerSwitchService.GetFromServer())
        {
            return await ServerGetReplays(request, token);
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

    public async Task<List<EventListDto>> GetTournaments()
    {
        return await replayRepository.GetTournaments();
    }

    public async Task<StatsResponse> GetStats(StatsRequest request, CancellationToken token = default)
    {
        if (fromServerSwitchService.GetFromServer())
        {
            return await ServerGetStats(request, token);
        }
        else
        {
            return await statsService.GetStatsResponse(request);
        }
    }

    public async Task<BuildResponse> GetBuild(BuildRequest request, CancellationToken token = default)
    {
        if (fromServerSwitchService.GetFromServer())
        {
            return await ServerGetBuild(request, token);
        }
        else
        {
            return await buildService.GetBuild(request, token);
        }
    }

    public async Task<int> GetRatingsCount(RatingsRequest request, CancellationToken token = default)
    {
        if (fromServerSwitchService.GetFromServer())
        {
            return await ServerGetRatingsCount(request, token);
        }
        else
        {
            return await ratingRepository.GetRatingsCount(request, token);
        }
    }

    public async Task<RatingsResult> GetRatings(RatingsRequest request, CancellationToken token = default)
    {
        if (fromServerSwitchService.GetFromServer())
        {
            return await ServerGetRatings(request, token);
        }
        else
        {
            return await ratingRepository.GetRatings(request, token);
        }
    }

    public async Task<List<MmrDevDto>> GetRatingsDeviation()
    {
        return await ratingRepository.GetRatingsDeviation();
    }

    public async Task<PlayerDetailDto> GetPlayerDetails(int toonId, CancellationToken token)
    {
        if (fromServerSwitchService.GetFromServer())
        {
            return await ServerGetPlayerDetails(toonId, token);
        }
        else
        {
            return await statsService.GetPlayerDetails(toonId, token);
        }
    }

    public async Task<PlayerDetailsResult> GetPlayerDetailsNg(int toonId, int rating, CancellationToken token)
    {
        if (fromServerSwitchService.GetFromServer())
        {
            return await ServerGetPlayerDetailsNg(toonId, rating, token);
        } else
        {
            return await statsService.GetPlayerDetails(toonId, (RatingType)rating, token);
        }
    }

    public async Task<PlayerDetailsGroupResult> GetPlayerGroupDetails(int toonId, int rating, CancellationToken token)
    {
        if (fromServerSwitchService.GetFromServer())
        {
            return await ServerGetPlayerGroupDetails(toonId, rating, token);
        }
        else
        {
            return await statsService.GetPlayerGroupDetails(toonId, (RatingType)rating, token);
        }
    }

    public async Task<List<PlayerMatchupInfo>> GetPlayerMatchups(int toonId, int rating, CancellationToken token)
    {
        if (fromServerSwitchService.GetFromServer())
        {
            return await ServerGetPlayerMatchups(toonId, rating, token);
        }
        else
        {
            return await statsService.GetPlayerMatchups(toonId, (RatingType)rating, token);
        }
    }

    public async Task<List<MmrDevDto>> GetRatingsDeviationStd()
    {
        return await ratingRepository.GetRatingsDeviationStd();
    }

    public async Task<List<RequestNames>> GetTopPlayers(bool std)
    {
        if (fromServerSwitchService.GetFromServer())
        {
            return await ServerGetTopPlayers(std);
        }
        else
        {
            return await buildService.GetTopPlayers(std, 25);
        }
    }

    public async Task<CmdrResult> GetCmdrInfo(CmdrRequest request, CancellationToken token = default)
    {
        return await Task.FromResult(new CmdrResult());
    }

    public async Task<CrossTableResponse> GetCrossTable(CrossTableRequest request, CancellationToken token = default)
    {
        return await Task.FromResult(new CrossTableResponse());
    }

    public async Task<List<BuildResponseReplay>> GetTeamReplays(CrossTableReplaysRequest request, CancellationToken token = default)
    {
        return await Task.FromResult(new List<BuildResponseReplay>());
    }

    public async Task<ToonIdRatingResponse> GetToonIdRatings(ToonIdRatingRequest request, CancellationToken token)
    {
        return await ServerGetToonIdRatings(request, token);
    }

    public async Task<int> GetEventReplaysCount(ReplaysRequest request, CancellationToken token = default)
    {
        return await Task.FromResult(0);
    }

    public async Task<ICollection<ReplayListEventDto>> GetEventReplays(ReplaysRequest request, CancellationToken token = default)
    {
        return await Task.FromResult(new List<ReplayListEventDto>());
    }

    public async Task<StatsResponse> GetTourneyStats(StatsRequest request, CancellationToken token = default)
    {
        return await Task.FromResult(new StatsResponse());
    }

    public async Task<FunStats> GetFunStats(List<int> toonIds)
    {
        return await statsService.GetFunStats(toonIds);
    }

    public async Task<StatsUpgradesResponse> GetUpgradeStats(BuildRequest buildRequest, CancellationToken token)
    {
        return await Task.FromResult(new StatsUpgradesResponse());
    }
}
