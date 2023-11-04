using dsstats.shared;
using dsstats.shared.Interfaces;
using System.Net.Http.Json;

namespace dsstats.webclient.Services;

public class WinrateService : IWinrateService
{
    private readonly HttpClient httpClient;
    private readonly ILogger<WinrateService> logger;
    private readonly string statsController = "api/v1/Stats";

    public WinrateService(HttpClient httpClient, ILogger<WinrateService> logger)
    {
        this.httpClient = httpClient;
        this.logger = logger;
    }

    public async Task<WinrateResponse> GetWinrate(StatsRequest request, CancellationToken token)
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
            logger.LogError("failed getting winrate: {error}", ex.Message);
        }
        return new();
    }

    public async Task<WinrateResponse> GetWinrate(WinrateRequest request, CancellationToken token)
    {
        return await GetWinrate(request as StatsRequest, token);
    }
}
