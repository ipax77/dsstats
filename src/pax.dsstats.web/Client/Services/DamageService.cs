using pax.dsstats.shared;
using pax.dsstats.shared.Interfaces;
using System.Net.Http.Json;

namespace pax.dsstats.web.Client.Services;

public class DamageService : IDamageService
{
    private readonly HttpClient httpClient;
    private readonly ILogger<DamageService> logger;
    private readonly string statsController = "api/v6/Stats";

    public DamageService(HttpClient httpClient, ILogger<DamageService> logger)
    {
        this.httpClient = httpClient;
        this.logger = logger;
    }


    public async Task<DamageResponse> GetDamage(DamageRequest request, CancellationToken token = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync($"{statsController}/damage", request);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadFromJsonAsync<DamageResponse>();
            if (content != null)
            {
                return content;
            }
        }
        catch (Exception ex)
        {
            logger.LogError($"failed getting damage: {ex.Message}");
        }
        return new();
    }
}
