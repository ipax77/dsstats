
using Microsoft.Extensions.Logging;
using pax.dsstats.shared;
using System.Net.Http.Json;

namespace sc2dsstats.maui.Services;

public partial class DataService
{
    private readonly string statsController = "api/v3/Stats/";
    private readonly string buildsController = "api/v2/Builds/";
    private readonly string ratingController = "api/Ratings/";

    public async Task<ReplayDetailsDto?> ServerGetDetailReplay(string replayHash, CancellationToken token = default)
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

    public async Task<ReplayDto?> ServerGetReplay(string replayHash, CancellationToken token = default)
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

    public async Task<int> ServerGetReplaysCount(ReplaysRequest request, CancellationToken token = default)
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

    public async Task<ICollection<ReplayListDto>> ServerGetReplays(ReplaysRequest request, CancellationToken token = default)
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


    public async Task<StatsResponse> ServerGetStats(StatsRequest request, CancellationToken token = default)
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

    public async Task<BuildResponse> ServerGetBuild(BuildRequest request, CancellationToken token = default)
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

    public async Task<int> ServerGetRatingsCount(RatingsRequest request, CancellationToken token = default)
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

    public async Task<RatingsResult> ServerGetRatings(RatingsRequest request, CancellationToken token = default)
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

    public async Task<List<RavenPlayerDto>> ServerGetPlayerRatings(int toonId)
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

    public async Task<List<MmrDevDto>> ServerGetRatingsDeviation()
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

    public async Task<List<MmrDevDto>> ServerGetRatingsDeviationStd()
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

    public async Task<PlayerDetailDto> ServerGetPlayerDetails(int toonId, CancellationToken token)
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

    public async Task<PlayerDetailsResult> ServerGetPlayerDetailsNg(int toonId, int rating, CancellationToken token)
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

    public async Task<PlayerDetailsGroupResult> ServerGetPlayerGroupDetails(int toonId, int rating, CancellationToken token)
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

    public async Task<List<PlayerMatchupInfo>> ServerGetPlayerMatchups(int toonId, int rating, CancellationToken token)
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

    public async Task<List<RequestNames>> ServerGetTopPlayers(bool std)
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

    public async Task<CmdrResult> ServerGetCmdrInfo(CmdrRequest request, CancellationToken token = default)
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

    public async Task<CrossTableResponse> ServerGetCrossTable(CrossTableRequest request, CancellationToken token = default)
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

    public async Task<ToonIdRatingResponse> ServerGetToonIdRatings(ToonIdRatingRequest request, CancellationToken token)
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

    public async Task<List<PlayerRatingReplayCalcDto>> ServerGetToonIdCalcRatings(ToonIdRatingRequest request, CancellationToken token)
    {
        try
        {
            var result = await httpClient.PostAsJsonAsync($"{ratingController}GetToonIdCalcRatings", request);
            if (result.IsSuccessStatusCode)
            {
                var toonIdRatingResponse = await result.Content.ReadFromJsonAsync<List<PlayerRatingReplayCalcDto>>();
                if (toonIdRatingResponse != null)
                {
                    return toonIdRatingResponse;
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            logger.LogError($"failed getting toonIdCalcRatings: {ex.Message}");
        }
        return new();
    }

    public async Task<GameInfoResult> ServerGetGameInfo(GameInfoRequest request, CancellationToken token)
    {
        try
        {
            var result = await httpClient.PostAsJsonAsync($"{statsController}GetGameInfo", request);
            if (result.IsSuccessStatusCode)
            {
                var infoResult = await result.Content.ReadFromJsonAsync<GameInfoResult>();
                if (infoResult != null)
                {
                    return infoResult;
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
}
