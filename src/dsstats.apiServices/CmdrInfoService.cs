using dsstats.shared;
using dsstats.shared.Interfaces;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace dsstats.apiServices;

public class CmdrInfoService : ICmdrInfoService
{
    private readonly HttpClient httpClient;
    private readonly ILogger<CmdrInfoService> logger;
    private readonly string cmdrInfoController = "api8/v1/cmdrinfo";

    public CmdrInfoService(HttpClient httpClient, ILogger<CmdrInfoService> logger)
    {
        this.httpClient = httpClient;
        this.logger = logger;
    }

    public async Task<List<CmdrPlayerInfo>> GetCmdrPlayerInfos(CmdrInfoRequest request, CancellationToken token = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync(cmdrInfoController, request, token);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<CmdrPlayerInfo>>() ?? new();
        }
        catch (Exception ex)
        {
            logger.LogError("failed getting cmdr player infos: {error}", ex.Message);
        }
        return new();
    }
}
