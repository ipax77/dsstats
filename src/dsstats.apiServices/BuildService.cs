using dsstats.shared;
using dsstats.shared.Interfaces;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace dsstats.apiServices;

public class BuildService : IBuildService
{
    private readonly HttpClient httpClient;
    private readonly ILogger<BuildService> logger;
    private readonly string buildController = "api8/v1/builds";

    public BuildService(HttpClient httpClient, ILogger<BuildService> logger)
    {
        this.httpClient = httpClient;
        this.logger = logger;
    }

    public async Task<BuildResponse> GetBuild(BuildRequest request, CancellationToken token = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync($"{buildController}", request, token);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<BuildResponse>() ?? new();
        }
        catch (Exception ex)
        {
            logger.LogError("failed getting build: {error}", ex.Message);
        }
        return new();
    }

    public async Task<List<RequestNames>> GetDefaultPlayers()
    {
        try
        {
            return await httpClient.GetFromJsonAsync<List<RequestNames>>($"{buildController}/defaultplayers") ?? new();
        }
        catch (Exception ex)
        {
            logger.LogError("failed getting default players: {error}", ex.Message);
        }
        return new();
    }

    public async Task<List<RequestNames>> GetTopPlayers(RatingType ratingType)
    {
        try
        {
            return await httpClient
             .GetFromJsonAsync<List<RequestNames>>($"{buildController}/topplayers/{(int)ratingType}") ?? new();
        }
        catch (Exception ex)
        {
            logger.LogError("failed getting top players: {error}", ex.Message);
        }
        return new();
    }

    public async Task<List<ReplayListDto>> GetReplays(BuildRequest request, int skip, int take, CancellationToken token)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync($"{buildController}/replays/{skip}/{take}", request, token);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<ReplayListDto>>() ?? new();
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            logger.LogError("failed getting build replays: {erro}", ex.Message);
        }
        return new();
    }

    public async Task<int> GetReplaysCount(BuildRequest request, CancellationToken token = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync($"{buildController}/replayscount", request, token);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<int>();
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            logger.LogError("failed getting build replays count: {error}", ex.Message);
        }
        return 0;
    }

    public async Task<BuildMapResponse> GetReplayBuildMap(BuildRequest request, CancellationToken token = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync($"{buildController}/replaybuildmap", request, token);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<BuildMapResponse>() ?? new();
        }
        catch (Exception ex)
        {
            logger.LogError("failed getting build map: {error}", ex.Message);
        }
        return new();
    }
}