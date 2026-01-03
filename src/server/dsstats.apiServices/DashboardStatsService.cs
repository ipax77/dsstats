using dsstats.shared;
using dsstats.shared.Interfaces;
using System.Net.Http.Json;

namespace dsstats.apiServices;

public class DashboardStatsService(IHttpClientFactory httpClientFactory) : IDashboardStatsService
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("api");

    public async Task<DashboardStatsResponse> GetDashboardStatsAsync(CancellationToken token = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("api10/Stats", token);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<DashboardStatsResponse>(cancellationToken: token) ?? new();
        }
        catch
        {
            return new();
        }
    }
}
