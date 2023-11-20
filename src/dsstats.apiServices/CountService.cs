using dsstats.shared;
using dsstats.shared.Interfaces;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace dsstats.apiServices;

public class CountService : ICountService
{
    private readonly HttpClient httpClient;
    private readonly ILogger<CountService> logger;
    private readonly string statsController = "api/v1/stats";

    public CountService(HttpClient httpClient, ILogger<CountService> logger)
    {
        this.httpClient = httpClient;
        this.logger = logger;
    }

    public async Task<CountResponse> GetCount(StatsRequest request, CancellationToken token = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync($"{statsController}/count", request, token);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<CountResponse>() ?? new();
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            logger.LogError("failed getting synergy: {error}", ex.Message);
        }
        return new();
    }
}
