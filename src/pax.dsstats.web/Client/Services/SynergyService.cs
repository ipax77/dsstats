using Microsoft.Extensions.Logging;
using pax.dsstats.shared;
using pax.dsstats.shared.Interfaces;
using System.Net.Http;
using System.Net.Http.Json;

namespace pax.dsstats.web.Client.Services;

public class SynergyService : ISynergyService
{
    private readonly HttpClient httpClient;
    private readonly ILogger<SynergyService> logger;
    private readonly string statsController = "api/v6/Stats";

    public SynergyService(HttpClient httpClient, ILogger<SynergyService> logger)
    {
        this.httpClient = httpClient;
        this.logger = logger;
    }

    public async Task<SynergyResponse> GetSynergy(SynergyRequest request, CancellationToken token = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync($"{statsController}/synergy", request, token);
            response.EnsureSuccessStatusCode();
            var data = await response.Content.ReadFromJsonAsync<SynergyResponse>();

            if (data == null)
            {
                logger.LogError($"failed getting synergy");
            }
            else
            {
                return data;
            }
        }
        catch (Exception ex)
        {
            logger.LogError($"failed getting synergy: {ex.Message}");
        }
        return new();
    }
}
