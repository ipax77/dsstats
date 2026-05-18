using dsstats.shared;
using dsstats.shared.Interfaces;
using System.Net.Http.Json;

namespace dsstats.apiServices;

public sealed class BuildDetailsService(IHttpClientFactory httpClientFactory) : IBuildDetailsService
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("api");

    public async Task<List<BuildDetailsOverviewRow>> GetOverview(BuildDetailsRequest request, CancellationToken token = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api10/BuildDetails/overview", request, token);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<BuildDetailsOverviewRow>>(cancellationToken: token) ?? [];
        }
        catch (Exception)
        {
            return [];
        }
    }

    public async Task<List<BuildDetailsMatchupRow>> GetMatchups(BuildDetailsMatchupRequest request, CancellationToken token = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api10/BuildDetails/matchups", request, token);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<BuildDetailsMatchupRow>>(cancellationToken: token) ?? [];
        }
        catch (Exception)
        {
            return [];
        }
    }

    public async Task<List<BuildDetailsSampleReplay>> GetSampleReplays(BuildDetailsSamplesRequest request, CancellationToken token = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api10/BuildDetails/samples", request, token);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<BuildDetailsSampleReplay>>(cancellationToken: token) ?? [];
        }
        catch (Exception)
        {
            return [];
        }
    }

    public async Task<List<BuildDetailsTeamBuildOverviewRow>> GetTeamBuildOverview(BuildDetailsRequest request, CancellationToken token = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api10/BuildDetails/team-builds/overview", request, token);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<BuildDetailsTeamBuildOverviewRow>>(cancellationToken: token) ?? [];
        }
        catch (Exception)
        {
            return [];
        }
    }

    public async Task<List<BuildDetailsTeamBuildSampleReplay>> GetTeamBuildSampleReplays(BuildDetailsTeamBuildSamplesRequest request, CancellationToken token = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api10/BuildDetails/team-builds/samples", request, token);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<BuildDetailsTeamBuildSampleReplay>>(cancellationToken: token) ?? [];
        }
        catch (Exception)
        {
            return [];
        }
    }

    public async Task<List<BuildDetailsRaceRosterOverviewRow>> GetRaceRosterOverview(BuildDetailsRequest request, CancellationToken token = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api10/BuildDetails/race-rosters/overview", request, token);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<BuildDetailsRaceRosterOverviewRow>>(cancellationToken: token) ?? [];
        }
        catch (Exception)
        {
            return [];
        }
    }

    public async Task<List<BuildDetailsRaceRosterMatchupRow>> GetRaceRosterMatchups(BuildDetailsRaceRosterMatchupRequest request, CancellationToken token = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api10/BuildDetails/race-rosters/matchups", request, token);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<BuildDetailsRaceRosterMatchupRow>>(cancellationToken: token) ?? [];
        }
        catch (Exception)
        {
            return [];
        }
    }

    public async Task<List<BuildDetailsRaceRosterSampleReplay>> GetRaceRosterSampleReplays(BuildDetailsRaceRosterSamplesRequest request, CancellationToken token = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api10/BuildDetails/race-rosters/samples", request, token);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<BuildDetailsRaceRosterSampleReplay>>(cancellationToken: token) ?? [];
        }
        catch (Exception)
        {
            return [];
        }
    }
}
