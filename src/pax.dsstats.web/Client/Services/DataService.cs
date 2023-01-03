using MathNet.Numerics.LinearAlgebra.Factorization;
using pax.dsstats.shared;
using pax.dsstats.shared.Raven;
using System.Net.Http.Json;

namespace pax.dsstats.web.Client.Services;

public class DataService : IDataService
{
    private readonly HttpClient httpClient;
    private readonly ILogger<DataService> logger;
    private readonly string statsController = "api/Stats/";
    private readonly string buildsController = "api/Builds/";
    private readonly string ratingController = "api/Ratings/";

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
        catch(OperationCanceledException) { }
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

    public async Task<ToonIdRatingResponse> GetToonIdRatings(ToonIdRatingRequest request, CancellationToken token)
    {
        try
        {
            var result = await httpClient.PostAsJsonAsync($"{ratingController}GetToonIdRatings", request);
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
            logger.LogError($"failed getting toonIdRatings: {ex.Message}");
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
}
