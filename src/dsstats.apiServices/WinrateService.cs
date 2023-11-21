
using dsstats.shared;
using dsstats.shared.Interfaces;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace dsstats.apiServices;

public class WinrateService : IWinrateService
{
    private readonly HttpClient httpClient;
    private readonly ILogger<WinrateService> logger;
    private readonly string statsController = "api8/v1/stats";

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
        catch (OperationCanceledException) { }
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

