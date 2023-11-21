using dsstats.shared;
using dsstats.shared.Interfaces;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace dsstats.apiServices;

public class SynergyService : ISynergyService
{
    private readonly HttpClient httpClient;
    private readonly ILogger<SynergyService> logger;
    private readonly string statsController = "api8/v1/stats";

    public SynergyService(HttpClient httpClient, ILogger<SynergyService> logger)
    {
        this.httpClient = httpClient;
        this.logger = logger;
    }

    public async Task<SynergyResponse> GetSynergy(StatsRequest request, CancellationToken token = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync($"{statsController}/synergy", request, token);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<SynergyResponse>() ?? new();
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            logger.LogError("failed getting synergy: {error}", ex.Message);
        }
        return new();
    }
}
