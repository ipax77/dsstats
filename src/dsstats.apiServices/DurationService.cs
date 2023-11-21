
using dsstats.shared;
using dsstats.shared.Interfaces;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace dsstats.apiServices;

public class DurationService : IDurationService
{
    private readonly HttpClient httpClient;
    private readonly ILogger<DurationService> logger;
    private readonly string statsController = "api8/v1/stats";

    public DurationService(HttpClient httpClient, ILogger<DurationService> logger)
    {
        this.httpClient = httpClient;
        this.logger = logger;
    }

    public async Task<DurationResponse> GetDuration(StatsRequest request, CancellationToken token)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync($"{statsController}/duration", request, token);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<DurationResponse>() ?? new();
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            logger.LogError("failed getting duration: {error}", ex.Message);
        }
        return new();
    }
}

