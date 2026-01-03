using dsstats.shared;
using dsstats.shared.Interfaces;
using System.Net.Http.Json;

namespace dsstats.apiServices;

public class PlayerService(IHttpClientFactory httpClientFactory) : IPlayerService
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("api");

    public async Task<List<PlayerRatingListItem>> GetRatings(PlayerRatingsRequest request, CancellationToken token = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api10/Players/ratings", request, token);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<PlayerRatingListItem>>(cancellationToken: token) ?? [];
        }
        catch (Exception)
        {
            return [];
        }
    }

    public async Task<int> GetRatingsCount(PlayerRatingsRequest request, CancellationToken token = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api10/Players/ratingscount", request, token);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<int>(cancellationToken: token);
        }
        catch (Exception)
        {
            return 0;
        }
    }

    public async Task<PlayerStatsResponse> GetPlayerStats(PlayerStatsRequest request, CancellationToken token = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api10/Players/stats", request, token);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<PlayerStatsResponse>(cancellationToken: token) ?? new PlayerStatsResponse();
        }
        catch (Exception)
        {
            return new PlayerStatsResponse();
        }
    }

    public async Task<RatingDetails> GetRatingDetails(PlayerStatsRequest request, CancellationToken token = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api10/Players/details", request, token);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<RatingDetails>(cancellationToken: token) ?? new RatingDetails();
        }
        catch (Exception)
        {
            return new RatingDetails();
        }
    }

    public async Task<CmdrAvgGainResponse> GetCommandersPerformance(PlayerStatsRequest request, CancellationToken token)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api10/Players/cmdrperf", request, token);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<CmdrAvgGainResponse>(cancellationToken: token) ?? new CmdrAvgGainResponse();
        }
        catch (Exception)
        {
            return new CmdrAvgGainResponse();
        }
    }

    public async Task<CmdrStrenghtResponse> GetCmdrPlayerInfos(CmdrStrenghtRequest request, CancellationToken token = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api10/Players/cmdrstrength", request, token);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<CmdrStrenghtResponse>(cancellationToken: token) ?? new CmdrStrenghtResponse();
        }
        catch (Exception)
        {
            return new CmdrStrenghtResponse();
        }
    }

    public async Task<DistributionResponse> GetRatingDistribution(DistributionRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api10/Players/ratingdistribution", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<DistributionResponse>() ?? new DistributionResponse();
        }
        catch (Exception)
        {
            return new DistributionResponse();
        }
    }
}
