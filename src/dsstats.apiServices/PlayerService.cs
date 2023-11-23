
using dsstats.shared;
using dsstats.shared.Interfaces;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace dsstats.apiServices;

public class PlayerService : IPlayerService
{
    private readonly HttpClient httpClient;
    private readonly ILogger<PlayerService> logger;
    private readonly string playerController = "api8/v1/player";

    public PlayerService(HttpClient httpClient, ILogger<PlayerService> logger)
    {
        this.httpClient = httpClient;
        this.logger = logger;
    }

    public async Task<string?> GetPlayerIdName(PlayerId playerId)
    {
        try
        {
            var result = await httpClient
             .GetAsync($"{playerController}/playeridname/{playerId.ToonId}/{playerId.RealmId}/{playerId.RegionId}");

            result.EnsureSuccessStatusCode();
            return await result.Content.ReadAsStringAsync();
        }
        catch (Exception ex)
        {
            logger.LogError("failed getting playerId name: {error}", ex.Message);
        }
        return null;
    }

    public async Task<int> GetRatingsCount(RatingsRequest request, CancellationToken token)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync($"{playerController}/ratingscount", request, token);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<int>(token);
        }
        catch (Exception ex)
        {
            logger.LogError("failed getting ratings count: {error}", ex.Message);
        }
        return 0;
    }

    public async Task<List<ComboPlayerRatingDto>> GetRatings(RatingsRequest request, CancellationToken token)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync($"{playerController}/ratings", request, token);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<ComboPlayerRatingDto>>(token) ?? new();
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            logger.LogError("failed getting ratings count: {error}", ex.Message);
        }
        return new();
    }

    public async Task<PlayerDetailSummary> GetPlayerPlayerIdSummary(PlayerId playerId,
                                                                    RatingType ratingType,
                                                                    RatingCalcType ratingCalcType,
                                                                    CancellationToken token = default)
    {
        try
        {
            var request = $"{playerController}/summary/{playerId.ToonId}/{playerId.RegionId}/{playerId.RealmId}/{(int)ratingType}/{(int)ratingCalcType}";
            var response = await httpClient.GetFromJsonAsync<PlayerDetailSummary>(request, token);
            if (response is not null)
            {
                return response;
            }
        }
        catch (Exception ex)
        {
            logger.LogError("Failed getting player summary: {error}", ex.Message);
        }
        return new();
    }

    public async Task<PlayerRatingDetails> GetPlayerIdPlayerRatingDetails(PlayerId playerId,
                                                                          RatingType ratingType,
                                                                          RatingCalcType ratingCalcType,
                                                                          CancellationToken token = default)
    {
        try
        {
            return await httpClient.GetFromJsonAsync<PlayerRatingDetails>($"{playerController}/rating/{playerId.ToonId}/{playerId.RegionId}/{playerId.RealmId}/{(int)ratingType}/{(int)ratingCalcType}", token) ?? new PlayerRatingDetails();
        }
        catch (Exception ex)
        {
            logger.LogError("Failed getting player rating details: {error}", ex.Message);
            return new PlayerRatingDetails();
        }
    }

    public async Task<List<PlayerCmdrAvgGain>> GetPlayerIdPlayerCmdrAvgGain(PlayerId playerId, RatingType ratingType, TimePeriod timePeriod, CancellationToken token)
    {
        try
        {
            return await httpClient
            .GetFromJsonAsync<List<PlayerCmdrAvgGain>>($"{playerController}/cmdravggain/{playerId.ToonId}/{playerId.RegionId}/{playerId.RealmId}/{(int)ratingType}/{(int)timePeriod}", token) ?? new();
        }
        catch (Exception ex)
        {
            logger.LogError("Failed getting player CmdrAvgGain: {error}", ex.Message);
            return new List<PlayerCmdrAvgGain>();
        }
    }

    public async Task<PlayerDetailResponse> GetPlayerIdPlayerDetails(PlayerDetailRequest request, CancellationToken token = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync($"{playerController}/details", request, token);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<PlayerDetailResponse>();
            if (result is not null)
            {
                return result;
            }
        }
        catch (Exception ex)
        {
            logger.LogError("Failed getting player details: {error}", ex.Message);
        }
        return new();
    }

    public async Task<List<ReplayPlayerChartDto>> GetPlayerRatingChartData(PlayerId playerId,
                                                                           RatingType ratingType,
                                                                           CancellationToken token = default)
    {
        try
        {
            var response =
             await httpClient.PostAsJsonAsync($"{playerController}/playerratingchartdata/{(int)ratingType}", playerId);

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<ReplayPlayerChartDto>>() ?? new();
        }
        catch (Exception ex)
        {
            logger.LogError("Failed getting player rating chart data: {error}", ex.Message);
        }
        return new();
    }

    public async Task<List<ReplayPlayerChartDto>> GetPlayerRatingChartData(PlayerId playerId,
                                                                       RatingCalcType ratingCalcType,
                                                                       RatingType ratingType,
                                                                       CancellationToken token = default)
    {
        try
        {
            var response =
             await httpClient.PostAsJsonAsync($"{playerController}/playerratingchartdata/{(int)ratingType}/{(int)ratingCalcType}", playerId);

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<ReplayPlayerChartDto>>() ?? new();
        }
        catch (Exception ex)
        {
            logger.LogError("Failed getting player rating chart data: {error}", ex.Message);
        }
        return new();
    }

    public async Task<List<CommanderInfo>> GetPlayerIdCommandersPlayed(PlayerId playerId,
                                                                       RatingType ratingType,
                                                                       CancellationToken token)
    {
        try
        {
            var response =
             await httpClient.PostAsJsonAsync($"{playerController}/playercommandersplayed/{(int)ratingType}", playerId);

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<CommanderInfo>>() ?? new();
        }
        catch (Exception ex)
        {
            logger.LogError("Failed getting player cmdr counts: {error}", ex.Message);
        }
        return new();
    }

    public async Task<DistributionResponse> GetDistribution(DistributionRequest request)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync($"{playerController}/distribution", request);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<DistributionResponse>();
            if (result is not null)
            {
                return result;
            }
        }
        catch (Exception ex)
        {
            logger.LogError("Failed getting distribution: {error}", ex.Message);
        }
        return new();

    }
}