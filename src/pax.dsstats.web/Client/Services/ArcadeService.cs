using pax.dsstats.shared;
using pax.dsstats.shared.Arcade;
using System.Net.Http.Json;

namespace pax.dsstats.web.Client.Services;

public class ArcadeService : IArcadeService
{
    private readonly HttpClient httpClient;
    private readonly ILogger<ArcadeService> logger;
    private readonly string arcadeController = "api/v1/arcade/";

    public ArcadeService(HttpClient httpClient, ILogger<ArcadeService> logger)
    {
        this.httpClient = httpClient;
        this.logger = logger;
    }

    public async Task<List<ArcadePlayerRatingDto>> GetRatings(ArcadeRatingsRequest request, CancellationToken token)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync($"{arcadeController}ratings", request, token);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<List<ArcadePlayerRatingDto>>(cancellationToken: token) ?? new();
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

    public async Task<int> GetRatingsCount(ArcadeRatingsRequest request, CancellationToken token)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync($"{arcadeController}count", request, token);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<int>(cancellationToken: token);
            }
            else
            {
                logger.LogError($"failed getting ratings count: {response.StatusCode}");
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception e)
        {
            logger.LogError($"failed getting ratings count: {e.Message}");
        }
        return 0;
    }

    public async Task<DistributionResponse> GetDistribution(DistributionRequest request, CancellationToken token = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync($"{arcadeController}distribution", request, token);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<DistributionResponse>(cancellationToken: token) ?? new();
            }
            else
            {
                logger.LogError($"failed getting distrubution: {response.StatusCode}");
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception e)
        {
            logger.LogError($"failed getting distrubution: {e.Message}");
        }
        return new();
    }

    public async Task<int> GetReplayCount(ArcadeReplaysRequest request, CancellationToken token)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync($"{arcadeController}replayscount", request, token);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<int>(cancellationToken: token);
            }
            else
            {
                logger.LogError($"failed getting replays count: {response.StatusCode}");
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception e)
        {
            logger.LogError($"failed getting replays count: {e.Message}");
        }
        return 0;
    }

    public async Task<List<ArcadeReplayListDto>> GetArcadeReplays(ArcadeReplaysRequest request, CancellationToken token)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync($"{arcadeController}replays", request, token);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<List<ArcadeReplayListDto>>(cancellationToken: token) ?? new();
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
        return new();
    }

    public async Task<ArcadeReplayDto?> GetArcadeReplay(int id, CancellationToken token = default)
    {
        try
        {
            return await httpClient.GetFromJsonAsync<ArcadeReplayDto>($"{arcadeController}replay/{id}");
        }
        catch (OperationCanceledException) { }
        catch (Exception e)
        {
            logger.LogError($"failed getting replay {id}: {e.Message}");
        }
        return null;
    }

    public async Task<ArcadePlayerDetails> GetPlayerDetails(ArcadePlayerId playerId, CancellationToken token)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync($"{arcadeController}playerdetails", playerId, token);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ArcadePlayerDetails>(cancellationToken: token) ?? new();
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

    public async Task<ArcadePlayerDetails> GetPlayerDetails(int arcadePlayerId, CancellationToken token)
    {
        try
        {
            return await httpClient.GetFromJsonAsync<ArcadePlayerDetails>($"{arcadeController}playerdetails/{arcadePlayerId}") ?? new();
        }
        catch (OperationCanceledException) { }
        catch (Exception e)
        {
            logger.LogError($"failed getting replay {arcadePlayerId}: {e.Message}");
        }
        return new();
    }

    public async Task<ArcadePlayerMoreDetails> GetMorePlayerDatails(ArcadePlayerId playerId, RatingType ratingType, CancellationToken token)
    {
        try
        {
            var request = $"{arcadeController}moreplayerdetails/{(int)ratingType}";
            var response = await httpClient.PostAsJsonAsync(request, playerId, token);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ArcadePlayerMoreDetails>(cancellationToken: token) ?? new();
            }
            else
            {
                logger.LogError($"failed getting more player details: {response.StatusCode}");
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception e)
        {
            logger.LogError($"failed getting more player details: {e.Message}");
        }
        return new();
    }

    public async Task<List<ReplayPlayerChartDto>> GetPlayerRatingChartData(PlayerId playerId, RatingType ratingType)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync($"{arcadeController}playerratingchartdata/{(int)ratingType}", playerId);
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
}
