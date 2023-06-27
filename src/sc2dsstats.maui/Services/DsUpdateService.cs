using Microsoft.Extensions.Logging;
using pax.dsstats.shared;
using pax.dsstats.shared.Interfaces;
using pax.dsstats.shared.Services;
using System.Net.Http.Json;

namespace sc2dsstats.maui.Services;

public class DsUpdateService : IDsUpdateService
{
    private readonly HttpClient httpClient;
    private readonly ILogger<DsUpdateService> logger;
    private readonly string statsController = "api/v6/Stats";

    public DsUpdateService(ILogger<DsUpdateService> logger)
    {
        httpClient = new HttpClient();
        // httpClient.BaseAddress = new Uri("https://localhost:7174");
        httpClient.BaseAddress = new Uri("https://dsstats.pax77.org");
        httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        this.logger = logger;
    }

    public async Task<List<DsUpdateInfo>> GetDsUpdates(TimePeriod timePeriod, CancellationToken token = default)
    {
        try
        {
            var response = await httpClient.GetFromJsonAsync<List<DsUpdateInfo>>($"{statsController}/dsupdates/{(int)timePeriod}");
            if (response == null)
            {
                logger.LogError($"failed getting dsudpates.");
            }
            else
            {
                return response;
            }
        }
        catch (Exception ex)
        {
            logger.LogError($"failed getting dsudpates: {ex.Message}");
        }
        return new();
    }

    public void SeedDsUpdates()
    {
        throw new NotImplementedException();
    }
}
