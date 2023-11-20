using dsstats.shared;
using dsstats.shared.Interfaces;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace dsstats.apiServices;

public class TeamcompService : ITeamcompService
{
    private readonly HttpClient httpClient;
    private readonly ILogger<TeamcompService> logger;
    private readonly string statsController = "api/v1/stats";

    public TeamcompService(HttpClient httpClient, ILogger<TeamcompService> logger)
    {
        this.httpClient = httpClient;
        this.logger = logger;
    }

    public async Task<TeamcompResponse> GetTeamcompResult(TeamcompRequest request, CancellationToken token = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync($"{statsController}/teamcomp", request, token);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<TeamcompResponse>() ?? new();
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            logger.LogError("failed getting teamcomp: {error}", ex.Message);
        }
        return new();
    }

    public async Task<int> GetReplaysCount(TeamcompReplaysRequest request, CancellationToken token = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync($"{statsController}/teamcompreplayscount", request, token);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<int>();
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            logger.LogError("failed getting teamcomp replays count: {error}", ex.Message);
        }
        return new();
    }

    public async Task<List<ReplayListDto>> GetReplays(TeamcompReplaysRequest request, CancellationToken token)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync($"{statsController}/teamcompreplays", request, token);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<ReplayListDto>>() ?? new();
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            logger.LogError("failed getting teamcomp replays: {error}", ex.Message);
        }
        return new();
    }
}
