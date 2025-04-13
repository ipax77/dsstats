using dsstats.shared;
using dsstats.shared8.Interfaces;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace dsstats.api.Services;

public class ReplaysService(IHttpClientFactory httpClientFactory, ILogger<ReplaysService> logger) : IReplaysService
{
    private readonly string replaysController = "api/v1/replays";

    public async Task<List<ReplayListDto>> GetReplays(ReplaysRequest request, CancellationToken token)
    {
        try
        {
            var httpClient = httpClientFactory.CreateClient("dsstats8");
            var response = await httpClient.PostAsJsonAsync($"{replaysController}/get", request, token);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadFromJsonAsync<List<ReplayListDto>>(token);
            ArgumentNullException.ThrowIfNull(content);
            return content;
        }
        catch (Exception ex)
        {
            logger.LogError("Failed getting replays: {error}", ex.Message);
        }
        return [];
    }

    public async Task<int> GetReplaysCount(ReplaysRequest request, CancellationToken token)
    {
        try
        {
            var httpClient = httpClientFactory.CreateClient("dsstats8");
            var response = await httpClient.PostAsJsonAsync($"{replaysController}/getcount", request, token);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadFromJsonAsync<int>(token);
            logger.LogInformation("Got content: {content}", content);
            return content;
        }
        catch (Exception ex)
        {
            logger.LogError("Failed getting replays count: {error}", ex.Message);
        }
        return 0;
    }
}


