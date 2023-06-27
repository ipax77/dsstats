using pax.dsstats.shared;
using pax.dsstats.shared.Interfaces;
using pax.dsstats.shared.Services;
using System.Net.Http.Json;

namespace pax.dsstats.web.Client.Services;

public class DsUpdateService : IDsUpdateService
{
    private readonly HttpClient httpClient;
    private readonly ILogger<DsUpdateService> logger;
    private readonly string statsController = "api/v6/Stats";

    public DsUpdateService(HttpClient httpClient, ILogger<DsUpdateService> logger)
    {
        this.httpClient = httpClient;
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
