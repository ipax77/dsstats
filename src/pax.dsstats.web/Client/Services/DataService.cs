using pax.dsstats.shared;
using pax.dsstats.shared.Arcade;
using System.Net.Http.Json;

namespace pax.dsstats.web.Client.Services;

public class DataService : IDataService
{
    private readonly HttpClient httpClient;
    private readonly ILogger<DataService> logger;
    private readonly string statsController = "api/v6/Stats/";
    private readonly string buildsController = "api/v2/Builds/";
    private readonly string ratingController = "api/v1/Ratings/";

    public DataService(HttpClient httpClient, ILogger<DataService> logger)
    {
        this.httpClient = httpClient;
        this.logger = logger;
    }

    public void SetFromServer(bool fromServer)
    {
    }

    public bool GetFromServer()
    {
        return true;
    }

    public async Task<ReplayDetailsDto?> GetDetailReplay(string replayHash, CancellationToken token = default)
    {
        try
        {
            var response = await httpClient.GetAsync($"{statsController}GetDetailReplay/{replayHash}", token);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ReplayDetailsDto>();
            }
            else
            {
                logger.LogError($"failed getting replay: {response.StatusCode}");
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception e)
        {
            logger.LogError($"failed getting replay: {e.Message}");
        }
        return null;
    }

    public async Task<ReplayDto?> GetReplay(string replayHash, CancellationToken token = default)
    {
        try
        {
            var response = await httpClient.GetAsync($"{statsController}GetReplay/{replayHash}", token);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ReplayDto>();
            }
            else
            {
                logger.LogError($"failed getting replay: {response.StatusCode}");
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception e)
        {
            logger.LogError($"failed getting replay: {e.Message}");
        }
        return null;
    }

    public async Task<int> GetReplaysCount(ReplaysRequest request, CancellationToken token = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync($"{statsController}GetReplaysCount", request, token);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<int>();
            }
            else
            {
                logger.LogError($"failed getting replay count: {response.StatusCode}");
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception e)
        {
            logger.LogError($"failed getting replay count: {e.Message}");
        }
        return 0;
    }

    public async Task<ICollection<ReplayListDto>> GetReplays(ReplaysRequest request, CancellationToken token = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync($"{statsController}GetReplays", request, token);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ICollection<ReplayListDto>>() ?? new List<ReplayListDto>();
            }
            else
            {
                logger.LogError($"failed getting replays: {response.StatusCode}");
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception e)
        {
            logger.LogError($"failed getting replays: {e.Message}");
        }
        return new List<ReplayListDto>();
    }

    public async Task<int> GetEventReplaysCount(ReplaysRequest request, CancellationToken token = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync($"{statsController}GetEventReplaysCount", request, token);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<int>();
            }
            else
            {
                logger.LogError($"failed getting event replay count: {response.StatusCode}");
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception e)
        {
            logger.LogError($"failed getting event replay count: {e.Message}");
        }
        return 0;
    }

    public async Task<ICollection<ReplayListEventDto>> GetEventReplays(ReplaysRequest request, CancellationToken token = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync($"{statsController}GetEventReplays", request, token);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ICollection<ReplayListEventDto>>() ?? new List<ReplayListEventDto>();
            }
            else
            {
                logger.LogError($"failed getting event replays: {response.StatusCode}");
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception e)
        {
            logger.LogError($"failed getting event replays: {e.Message}");
        }
        return new List<ReplayListEventDto>();
    }

    public async Task<StatsResponse> GetStats(StatsRequest request, CancellationToken token = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync($"{statsController}GetStats", request, token);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<StatsResponse>() ?? new();
            }
            else
            {
                logger.LogError($"failed getting stats: {response.StatusCode}");
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception e)
        {
            logger.LogError($"failed getting stats: {e.Message}");
        }
        return new();
    }

    public async Task<StatsResponse> GetTourneyStats(StatsRequest request, CancellationToken token = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync($"{statsController}GetTourneyStats", request, token);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<StatsResponse>() ?? new();
            }
            else
            {
                logger.LogError($"failed getting stats: {response.StatusCode}");
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception e)
        {
            logger.LogError($"failed getting stats: {e.Message}");
        }
        return new();
    }

    public async Task<BuildResponse> GetBuild(BuildRequest request, CancellationToken token = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync($"{buildsController}GetBuild", request);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<BuildResponse>() ?? new();
            }
            else
            {
                logger.LogError($"failed getting build: {response.StatusCode}");
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception e)
        {
            logger.LogError($"failed getting build: {e.Message}");
        }
        return new();
    }

    public async Task<int> GetRatingsCount(RatingsRequest request, CancellationToken token = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync($"{ratingController}GetRatingsCount", request, token);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<int>(cancellationToken: token);
            }
            else
            {
                logger.LogError($"failed getting ratingscount: {response.StatusCode}");
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception e)
        {
            logger.LogError($"failed getting ratings count: {e.Message}");
        }
        return new();
    }

    public async Task<RatingsResult> GetRatings(RatingsRequest request, CancellationToken token = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync($"{ratingController}GetRatings", request, token);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<RatingsResult>(cancellationToken: token) ?? new();
            }
            else
            {
                logger.LogError($"failed getting ratings: {response.StatusCode}");
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception e)
        {
            logger.LogError($"failed getting ratings: {e.Message}");
        }
        return new();
    }

    public async Task<List<RavenPlayerDto>> GetPlayerRatings(int toonId)
    {
        try
        {
            var response = await httpClient.GetAsync($"{ratingController}GetPlayerRatings/{toonId}");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<List<RavenPlayerDto>>() ?? new();
            }
        }
        catch (Exception e)
        {
            logger.LogError($"failed getting player ratings: {e.Message}");
        }
        return new();
    }

    public async Task<List<MmrDevDto>> GetRatingsDeviation()
    {
        try
        {
            return await httpClient.GetFromJsonAsync<List<MmrDevDto>>($"{ratingController}GetRatingsDeviation") ?? new();
        }
        catch (Exception e)
        {
            logger.LogError($"failed getting rating deviation: {e.Message}");
        }
        return new();
    }

    public async Task<List<MmrDevDto>> GetRatingsDeviationStd()
    {
        try
        {
            return await httpClient.GetFromJsonAsync<List<MmrDevDto>>($"{ratingController}GetRatingsDeviationStd") ?? new();
        }
        catch (Exception e)
        {
            logger.LogError($"failed getting rating deviationstd: {e.Message}");
        }
        return new();
    }

    public async Task<PlayerDetailDto> GetPlayerDetails(int toonId, CancellationToken token)
    {
        try
        {
            var response = await httpClient.GetAsync($"{statsController}GetPlayerDetails/{toonId}", token);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<PlayerDetailDto>();
                return result ?? new();
            }
            else
            {
                logger.LogError($"failed getting playerDetails: {response.StatusCode}");
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            logger.LogError($"failed getting playerDetails: {ex.Message}");
        }
        return new();
    }

    public async Task<PlayerDetailsResult> GetPlayerDetailsNg(int toonId, int rating, CancellationToken token)
    {
        try
        {
            var response = await httpClient.GetAsync($"{statsController}GetPlayerDetailsNg/{toonId}/{rating}", token);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<PlayerDetailsResult>();
                return result ?? new();
            }
            else
            {
                logger.LogError($"failed getting playerDetails: {response.StatusCode}");
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            logger.LogError($"failed getting playerDetails: {ex.Message}");
        }
        return new();
    }

    public async Task<PlayerDetailsGroupResult> GetPlayerGroupDetails(int toonId, int rating, CancellationToken token)
    {
        try
        {
            var response = await httpClient.GetAsync($"{statsController}GetPlayerGroupDetails/{toonId}/{rating}", token);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<PlayerDetailsGroupResult>();
                return result ?? new();
            }
            else
            {
                logger.LogError($"failed getting playerGroupDetails: {response.StatusCode}");
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            logger.LogError($"failed getting playerGroupDetails: {ex.Message}");
        }
        return new();
    }

    public async Task<List<PlayerMatchupInfo>> GetPlayerMatchups(int toonId, int rating, CancellationToken token)
    {
        try
        {
            var response = await httpClient.GetAsync($"{statsController}GetPlayerMatchups/{toonId}/{rating}", token);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<List<PlayerMatchupInfo>>();
                return result ?? new();
            }
            else
            {
                logger.LogError($"failed getting GetPlayerMatchups: {response.StatusCode}");
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            logger.LogError($"failed getting GetPlayerMatchups: {ex.Message}");
        }
        return new();
    }

    public async Task<List<RequestNames>> GetTopPlayers(bool std)
    {
        try
        {
            var topPlayers = await httpClient.GetFromJsonAsync<List<RequestNames>>($"{buildsController}topplayers/{std}/1000");
            if (topPlayers != null)
            {
                return topPlayers;
            }
        }
        catch (Exception ex)
        {
            logger.LogError($"failed getting topPlayers: {ex.Message}");
        }
        return Data.GetDefaultRequestNames();
    }

    public async Task<CmdrResult> GetCmdrInfo(CmdrRequest request, CancellationToken token = default)
    {
        try
        {
            var result = await httpClient.PostAsJsonAsync($"{statsController}GetCmdrInfo", request);
            if (result.IsSuccessStatusCode)
            {
                var cmdrResult = await result.Content.ReadFromJsonAsync<CmdrResult>();
                if (cmdrResult != null)
                {
                    return cmdrResult;
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            logger.LogError($"failed getting cmdrInfo: {ex.Message}");
        }
        return new();
    }

    public async Task<CrossTableResponse> GetCrossTable(CrossTableRequest request, CancellationToken token = default)
    {
        try
        {
            var result = await httpClient.PostAsJsonAsync($"{statsController}GetCrosstable", request);
            if (result.IsSuccessStatusCode)
            {
                var cmdrResult = await result.Content.ReadFromJsonAsync<CrossTableResponse>();
                if (cmdrResult != null)
                {
                    return cmdrResult;
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            logger.LogError($"failed getting crosstable: {ex.Message}");
        }
        return new();
    }

    public async Task<List<BuildResponseReplay>> GetTeamReplays(CrossTableReplaysRequest request, CancellationToken token)
    {
        try
        {
            var result = await httpClient.PostAsJsonAsync($"{statsController}GetTeamReplays", request);
            if (result.IsSuccessStatusCode)
            {
                var teamReplays = await result.Content.ReadFromJsonAsync<List<BuildResponseReplay>>();
                if (teamReplays != null)
                {
                    return teamReplays;
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            logger.LogError($"failed getting team replays: {ex.Message}");
        }
        return new();
    }

    public async Task<ToonIdRatingResponse> GetPlayerIdRatings(PlayerIdRatingRequest request, CancellationToken token)
    {
        try
        {
            var result = await httpClient.PostAsJsonAsync($"{ratingController}GetPlayerIdRatings", request);
            if (result.IsSuccessStatusCode)
            {
                var toonIdRatingResponse = await result.Content.ReadFromJsonAsync<ToonIdRatingResponse>();
                if (toonIdRatingResponse != null)
                {
                    return toonIdRatingResponse;
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            logger.LogError($"failed getting playerIdRatings: {ex.Message}");
        }
        return new();
    }

    public async Task<ICollection<string>> GetReplayPaths()
    {
        return await Task.FromResult(new List<string>());
    }

    public async Task<List<EventListDto>> GetTournaments()
    {
        try
        {
            var tournaments = await httpClient.GetFromJsonAsync<List<EventListDto>>($"{statsController}GetTournaments");
            if (tournaments != null)
            {
                return tournaments;
            }
        }
        catch (Exception ex)
        {
            logger.LogError($"failed getting tournaments: {ex.Message}");
        }
        return new();
    }

    public async Task<FunStats> GetFunStats(List<int> toonIds)
    {
        return await Task.FromResult(new FunStats());
    }

    public async Task<StatsUpgradesResponse> GetUpgradeStats(BuildRequest buildRequest, CancellationToken token)
    {
        try
        {
            var result = await httpClient.PostAsJsonAsync($"{statsController}GetStatsUpgrades", buildRequest);
            if (result.IsSuccessStatusCode)
            {
                var response = await result.Content.ReadFromJsonAsync<StatsUpgradesResponse>();
                if (response != null)
                {
                    return response;
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            logger.LogError($"failed getting statsUpgrades: {ex.Message}");
        }
        return new();
    }

    public async Task<GameInfoResult> GetGameInfo(GameInfoRequest request, CancellationToken token)
    {
        try
        {
            var result = await httpClient.PostAsJsonAsync($"{statsController}GetGameInfo", request);
            if (result.IsSuccessStatusCode)
            {
                var response = await result.Content.ReadFromJsonAsync<GameInfoResult>();
                if (response != null)
                {
                    return response;
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            logger.LogError($"failed getting GameInfo: {ex.Message}");
        }
        return new();
    }

    public async Task<int> GetRatingChangesCount(RatingChangesRequest request, CancellationToken token = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync($"{ratingController}GetRatingChangesCount", request, token);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<int>(cancellationToken: token);
            }
            else
            {
                logger.LogError($"failed getting ratingchangescount: {response.StatusCode}");
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception e)
        {
            logger.LogError($"failed getting ratingChanges count: {e.Message}");
        }
        return new();
    }

    public async Task<RatingChangesResult> GetRatingChanges(RatingChangesRequest request, CancellationToken token = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync($"{ratingController}GetRatingChanges", request, token);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<RatingChangesResult>(cancellationToken: token) ?? new();
            }
            else
            {
                logger.LogError($"failed getting ratingChanges: {response.StatusCode}");
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception e)
        {
            logger.LogError($"failed getting ratingChanges: {e.Message}");
        }
        return new();
    }

    public async Task<List<PlayerRatingReplayCalcDto>> GetPlayerIdCalcRatings(PlayerIdRatingRequest request, CancellationToken token)
    {
        return await Task.FromResult(new List<PlayerRatingReplayCalcDto>());
    }

    public async Task<CmdrStrengthResult> GetCmdrStrengthResults(CmdrStrengthRequest request, CancellationToken token = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync($"{statsController}GetCmdrStrength", request, token);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<CmdrStrengthResult>(cancellationToken: token) ?? new();
            }
            else
            {
                logger.LogError($"failed getting cmdrStrength: {response.StatusCode}");
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception e)
        {
            logger.LogError($"failed getting cmdrStrength: {e.Message}");
        }
        return new();
    }

    public async Task<DistributionResponse> GetDistribution(DistributionRequest request, CancellationToken token = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync($"{ratingController}GetDistribution", request, token);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<DistributionResponse>(cancellationToken: token) ?? new();
            }
            else
            {
                logger.LogError($"failed getting distribution: {response.StatusCode}");
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception e)
        {
            logger.LogError($"failed getting distribution: {e.Message}");
        }
        return new();
    }

    public async Task<PlayerDetailResponse> GetPlayerDetails(PlayerDetailRequest request, CancellationToken token = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync($"{ratingController}GetPlayerDetails", request, token);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<PlayerDetailResponse>(cancellationToken: token) ?? new();
            }
            else
            {
                logger.LogError($"failed getting player details: {response.StatusCode}");
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception e)
        {
            logger.LogError($"failed getting player details: {e.Message}");
        }
        return new();
    }

    public async Task<PlayerDetailSummary> GetPlayerSummary(int toonId, CancellationToken token = default)
    {
        try
        {
            var result = await httpClient.GetFromJsonAsync<PlayerDetailSummary>($"{ratingController}GetPlayerDatailSummary/{toonId}", token);
            if (result != null)
            {
                return result;
            }
        }
        catch (Exception ex)
        {
            logger.LogError($"failed getting player summary: {ex.Message}");
        }
        return new();
    }

    public async Task<PlayerRatingDetails> GetPlayerRatingDetails(int toonId, RatingType ratingType, CancellationToken token = default)
    {
        try
        {
            var result = await httpClient.GetFromJsonAsync<PlayerRatingDetails>($"{ratingController}GetPlayerRatingDetails/{toonId}/{(int)ratingType}", token);
            if (result != null)
            {
                return result;
            }
        }
        catch (Exception ex)
        {
            logger.LogError($"failed getting player rating details: {ex.Message}");
        }
        return new();
    }

    public async Task<List<PlayerCmdrAvgGain>> GetPlayerCmdrAvgGain(int toonId, RatingType ratingType, TimePeriod timePeriod, CancellationToken token = default)
    {
        try
        {
            var result = await httpClient.GetFromJsonAsync<List<PlayerCmdrAvgGain>>($"{ratingController}GetPlayerCmdrAvgGain/{toonId}/{(int)ratingType}/{(int)timePeriod}", token);
            if (result != null)
            {
                return result;
            }
        }
        catch (Exception ex)
        {
            logger.LogError($"failed getting player cmdr avg gain: {ex.Message}");
        }
        return new();
    }

    public async Task<BuildRatingResponse> GetBuildByRating(BuildRatingRequest request, CancellationToken token = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync($"{buildsController}GetRatingBuild", request, token);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<BuildRatingResponse>(cancellationToken: token) ?? new();
            }
            else
            {
                logger.LogError($"failed getting buildbyrating: {response.StatusCode}");
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception e)
        {
            logger.LogError($"failed getting buildbyrating: {e.Message}");
        }
        return new();
    }

    public async Task<FunStatsResult> GetFunStats(FunStatsRequest request, CancellationToken token)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync($"{statsController}GetFunStats", request, token);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<FunStatsResult>(cancellationToken: token) ?? new();
            }
            else
            {
                logger.LogError($"failed getting funstats: {response.StatusCode}");
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception e)
        {
            logger.LogError($"failed getting funstats: {e.Message}");
        }
        return new();
    }

    public async Task<int> GetCmdrReplayInfosCount(CmdrInfoRequest request, CancellationToken token)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync($"{statsController}GetCmdrReplayInfosCount", request, token);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<int>(cancellationToken: token);
            }
            else
            {
                logger.LogError($"failed getting cmdrreplayinfos count: {response.StatusCode}");
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception e)
        {
            logger.LogError($"failed getting cmdrreplayinfos count: {e.Message}");
        }
        return new();
    }

    public async Task<List<ReplayCmdrInfo>> GetCmdrReplayInfos(CmdrInfoRequest request, CancellationToken token)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync($"{statsController}GetCmdrReplayInfos", request, token);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<List<ReplayCmdrInfo>>(cancellationToken: token) ?? new();
            }
            else
            {
                logger.LogError($"failed getting cmdrreplayinfos: {response.StatusCode}");
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception e)
        {
            logger.LogError($"failed getting cmdrreplayinfos: {e.Message}");
        }
        return new();
    }

    public async Task<List<CmdrPlayerInfo>> GetCmdrPlayerInfos(CmdrInfoRequest request, CancellationToken token)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync($"{statsController}GetCmdrPlayerInfos", request, token);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<List<CmdrPlayerInfo>>(cancellationToken: token) ?? new();
            }
            else
            {
                logger.LogError($"failed getting CmdrPlayerInfos: {response.StatusCode}");
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception e)
        {
            logger.LogError($"failed getting CmdrPlayerInfos: {e.Message}");
        }
        return new();
    }

    public async Task<int> GetCmdrReplaysCount(CmdrInfosRequest request, CancellationToken token)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync($"{statsController}GetCmdrReplaysCount", request, token);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<int>(cancellationToken: token);
            }
            else
            {
                logger.LogError($"failed getting CmdrReplaysCount count: {response.StatusCode}");
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception e)
        {
            logger.LogError($"failed getting CmdrReplaysCount count: {e.Message}");
        }
        return new();
    }

    public async Task<List<ReplayCmdrListDto>> GetCmdrReplays(CmdrInfosRequest request, CancellationToken token)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync($"{statsController}GetCmdrReplays", request, token);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<List<ReplayCmdrListDto>>(cancellationToken: token) ?? new();
            }
            else
            {
                logger.LogError($"failed getting CmdrReplays: {response.StatusCode}");
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception e)
        {
            logger.LogError($"failed getting CmdrReplays: {e.Message}");
        }
        return new();
    }

    public async Task<PlayerDetailResponse> GetIdPlayerDetails(PlayerDetailRequest request, CancellationToken token = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync($"{ratingController}GetIdPlayerDetails", request, token);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<PlayerDetailResponse>(cancellationToken: token) ?? new();
            }
            else
            {
                logger.LogError($"failed getting player details: {response.StatusCode}");
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception e)
        {
            logger.LogError($"failed getting player details: {e.Message}");
        }
        return new();
    }

    public async Task<PlayerDetailSummary> GetIdPlayerSummary(PlayerId playerId, CancellationToken token = default)
    {
        try
        {
            var result = await httpClient.GetFromJsonAsync<PlayerDetailSummary>($"{ratingController}GetIdPlayerDatailSummary/{playerId.ToonId}/{playerId.RegionId}/{playerId.RealmId}", token);
            if (result != null)
            {
                return result;
            }
        }
        catch (Exception ex)
        {
            logger.LogError($"failed getting player summary: {ex.Message}");
        }
        return new();
    }

    public async Task<PlayerRatingDetails> GetIdPlayerRatingDetails(PlayerId playerId, RatingType ratingType, CancellationToken token = default)
    {
        try
        {
            var result = await httpClient.GetFromJsonAsync<PlayerRatingDetails>($"{ratingController}GetIdPlayerRatingDetails/{playerId.ToonId}/{playerId.RegionId}/{playerId.RealmId}/{(int)ratingType}", token);
            if (result != null)
            {
                return result;
            }
        }
        catch (Exception ex)
        {
            logger.LogError($"failed getting player rating details: {ex.Message}");
        }
        return new();
    }

    public async Task<List<PlayerCmdrAvgGain>> GetIdPlayerCmdrAvgGain(PlayerId playerId, RatingType ratingType, TimePeriod timePeriod, CancellationToken token = default)
    {
        try
        {
            var result = await httpClient.GetFromJsonAsync<List<PlayerCmdrAvgGain>>($"{ratingController}GetIdPlayerCmdrAvgGain/{playerId.ToonId}/{playerId.RegionId}/{playerId.RealmId}/{(int)ratingType}/{(int)timePeriod}", token);
            if (result != null)
            {
                return result;
            }
        }
        catch (Exception ex)
        {
            logger.LogError($"failed getting player cmdr avg gain: {ex.Message}");
        }
        return new();
    }

    public async Task<List<ReplayPlayerChartDto>> GetPlayerRatingChartData(PlayerId playerId, RatingType ratingType)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync($"{statsController}playerratingchartdata/{(int)ratingType}", playerId);
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<List<ReplayPlayerChartDto>>();
                if (data != null)
                {
                    return data;
                }
            }
            else
            {
                logger.LogError($"failed getting playerchartdata: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            logger.LogError($"failed getting playerchartdata: {ex.Message}");
        }
        return new();
    }

    public async Task<ReplayRatingDto?> GetOnlineRating(ReplayDetailsDto replayDto)
    {
        return await Task.FromResult<ReplayRatingDto?>(null);
    }

    public async Task<ReplayRatingDto?> GetReplayComboRating(int replayId)
    {
        try
        {
            var result = await httpClient.GetFromJsonAsync<ReplayRatingDto?>($"{statsController}ComboReplayRating/{replayId}");
            return result;
        }
        catch (Exception ex)
        {
            logger.LogError($"failed getting ReplayComboRating {replayId}: {ex.Message}");
        }
        return null;
    }
}
