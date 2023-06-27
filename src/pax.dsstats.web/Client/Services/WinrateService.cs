using pax.dsstats.shared;
using pax.dsstats.shared.Interfaces;
using System.Net.Http.Json;

namespace pax.dsstats.web.Client.Services;

public class WinrateService : IWinrateService
{
    private readonly HttpClient httpClient;
    private readonly ILogger<WinrateService> logger;
    private readonly string statsController = "api/v6/Stats";

    public WinrateService(HttpClient httpClient, ILogger<WinrateService> logger)
    {
        this.httpClient = httpClient;
        this.logger = logger;
    }

    public async Task<WinrateResponse> GetWinrate(WinrateRequest request, CancellationToken token)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync($"{statsController}/winrate", request, token);
            response.EnsureSuccessStatusCode();
            var data = await response.Content.ReadFromJsonAsync<WinrateResponse>();

            if (data == null)
            {
                logger.LogError($"failed getting winrate");
            }
            else
            {
                return data;
            }
        }
        catch (Exception ex)
        {
            logger.LogError($"failed getting winrate: {ex.Message}");
        }
        return new();
    }
}
