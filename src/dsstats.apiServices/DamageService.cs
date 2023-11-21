
using dsstats.shared;
using dsstats.shared.Interfaces;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace dsstats.apiServices;

public class DamageService : IDamageService
{
    private readonly HttpClient httpClient;
    private readonly ILogger<DamageService> logger;
    private readonly string statsController = "api8/v1/stats";

    public DamageService(HttpClient httpClient, ILogger<DamageService> logger)
    {
        this.httpClient = httpClient;
        this.logger = logger;
    }

    public async Task<DamageResponse> GetDamage(StatsRequest request, CancellationToken token)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync($"{statsController}/damage", request, token);
            response.EnsureSuccessStatusCode();
            var data = await response.Content.ReadFromJsonAsync<DamageResponse>();

            if (data == null)
            {
                logger.LogError($"failed getting damage");
            }
            else
            {
                return data;
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            logger.LogError("failed getting damage: {error}", ex.Message);
        }
        return new();
    }
}

