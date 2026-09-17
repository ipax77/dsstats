using dsstats.shared;
using dsstats.shared.Interfaces;
using System.Net;
using System.Net.Http.Json;

namespace dsstats.apiServices;

public sealed class PlayerProfileService(IHttpClientFactory httpClientFactory) : IPlayerProfileService
{
    private readonly HttpClient client = httpClientFactory.CreateClient("api");

    public async Task<CmdrAvgGainResponse> GetCommanderPerformance(PlayerStatsRequest request, CancellationToken token = default)
    {
        using var response = await client.PostAsJsonAsync("api10/Players/cmdrperf", request, token);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CmdrAvgGainResponse>(cancellationToken: token)
            ?? throw new HttpRequestException("The player service returned an empty response.");
    }

    public Task<PlayerOverview?> GetOverview(PlayerProfileRequest request, CancellationToken token = default) =>
        Post<PlayerOverview>("overview", request, token);

    public Task<PlayerProfileDetails?> GetDetails(PlayerProfileRequest request, CancellationToken token = default) =>
        Post<PlayerProfileDetails>("details", request, token);

    private async Task<T?> Post<T>(string endpoint, PlayerProfileRequest request, CancellationToken token)
    {
        using var response = await client.PostAsJsonAsync($"api11/Players/{endpoint}", request, token);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return default;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: token)
            ?? throw new HttpRequestException("The player service returned an empty response.");
    }
}
