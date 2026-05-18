using dsstats.shared;
using dsstats.shared.Interfaces;
using dsstats.shared.Units;
using System.Net.Http.Json;

namespace dsstats.apiServices;

public class BuildsService(IHttpClientFactory httpClientFactory) : IBuildsService
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("api");

    public async Task<BuildsResponse> GetBuildResponse(BuildsRequest request, CancellationToken token = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api10/Builds", request, token);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<BuildsResponse>(cancellationToken: token) ?? new BuildsResponse();
        }
        catch (Exception)
        {
            return new BuildsResponse();
        }
    }

    public async Task<List<BuildUpgradeTimingDto>> GetUpgradeTimings(BuildsRequest request, bool includeAnecdotal = false, CancellationToken token = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"api10/Builds/upgrades/timing?includeAnecdotal={includeAnecdotal}", request, token);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<BuildUpgradeTimingDto>>(cancellationToken: token) ?? [];
        }
        catch (Exception)
        {
            return [];
        }
    }

    public async Task<List<BuildGasTimingDto>> GetGasTimings(BuildsRequest request, bool includeAnecdotal = false, CancellationToken token = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"api10/Builds/gas/timing?includeAnecdotal={includeAnecdotal}", request, token);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<BuildGasTimingDto>>(cancellationToken: token) ?? [];
        }
        catch (Exception)
        {
            return [];
        }
    }

    public async Task<List<DsUnitListDto>> GetUnits(DsUnitsRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api10/Builds/units", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<DsUnitListDto>>() ?? [];
        }
        catch (Exception)
        {
            return [];
        }
    }

    public async Task<DsUnitDto> GetUnit(int id)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<DsUnitDto>($"api10/Builds/unit/{id}");
            return response ?? new();
        }
        catch (Exception)
        {
            return new();
        }
    }
}
