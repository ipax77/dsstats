using pax.dsstats.shared;
using pax.dsstats.shared.Arcade;
using pax.dsstats.shared.Interfaces;
using System.Net.Http.Json;

namespace pax.dsstats.web.Client.Services;

public class ServerStatsService : IServerStatsService
{
    private readonly HttpClient httpClient;
    private readonly ILogger<ServerStatsService> logger;

    public ServerStatsService(HttpClient httpClient, ILogger<ServerStatsService> logger)
    {
        this.httpClient = httpClient;
        this.logger = logger;
    }

    private readonly string ServerStatsController = "/api/v1/ServerStats";

    public async Task<List<ServerStatsResult>> GetDsstatsStats()
    {
        try
        {
            var response = await httpClient.GetFromJsonAsync<List<ServerStatsResult>>($"{ServerStatsController}/dsstats");
            if (response != null)
            {
                return response;
            }
        }
        catch (Exception ex)
        {
            logger.LogError($"failed getting dsstats server stats: {ex.Message}");
        }
        return new();
    }

    public async Task<List<ServerStatsResult>> GetSc2ArcadeStats()
    {
        try
        {
            var response = await httpClient.GetFromJsonAsync<List<ServerStatsResult>>($"{ServerStatsController}/sc2arcade");
            if (response != null)
            {
                return response;
            }
        }
        catch (Exception ex)
        {
            logger.LogError($"failed getting arcade server stats: {ex.Message}");
        }
        return new();
    }

    public async Task<MergeResultReplays> GetMergeResultReplays(PlayerId playerId, CancellationToken token)
    {
        try 
        {
            var response = await httpClient.PostAsJsonAsync($"{ServerStatsController}/mergeresult", playerId, token);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<MergeResultReplays>(cancellationToken: token);
            if (result != null)
            {
                return result;
            }
        }
        catch (Exception ex)
        {
            logger.LogError($"failed getting mergeresult: {ex.Message}");
        }
        return new();
    }
}
