using dsstats.shared;
using dsstats.shared.Interfaces;
using System.Net.Http.Json;

namespace dsstats.apiServices;

public class StatsService(IHttpClientFactory httpClientFactory) : IStatsService
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("api");

    public async Task<T> GetStatsAsync<T>(StatsType type, StatsRequest request, CancellationToken token = default) where T : IStatsResponse
    {
        try
        {
            request.Type = type;
            var response = await _httpClient.PostAsJsonAsync("api10/Stats", request, token);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<T>(cancellationToken: token) ?? throw new Exception("Failed to deserialize response");;
        }
        catch (Exception)
        {
            throw;
        }
    }
}
