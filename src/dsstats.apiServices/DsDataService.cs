using dsstats.shared;
using dsstats.shared.Interfaces;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace dsstats.apiServices;

public class DsDataService(HttpClient httpClient, ILogger<DsDataService> logger) : IDsDataService
{
    private readonly string dsDataController = "api8/v1/dsdata";

    public async Task<DsUnitDto?> GetUnitDetails(UnitDetailRequest request, CancellationToken token = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync($"{dsDataController}/getunitdetails", request, token);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<DsUnitDto?>();
        }
        catch (Exception ex)
        {
            logger.LogError("failed getting units details: {error}", ex.Message);
        }
        return null;
    }

    public async Task<int> GetUnitId(UnitDetailRequest request)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync($"{dsDataController}/getunitid", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<int>();
        }
        catch (Exception ex)
        {
            logger.LogError("failed getting units id: {error}", ex.Message);
        }
        return 0;
    }

    public async Task<List<DsUnitListDto>> GetUnitsList(UnitRequest request, CancellationToken token = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync($"{dsDataController}/getunits", request, token);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<DsUnitListDto>>() ?? [];
        }
        catch (Exception ex)
        {
            logger.LogError("failed getting units: {error}", ex.Message);
        }
        return [];
    }

    public async Task<int> GetUnitsListCount(UnitRequest request, CancellationToken token = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync($"{dsDataController}/getunitscount", request, token);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<int>();
        }
        catch (Exception ex)
        {
            logger.LogError("failed getting units count: {error}", ex.Message);
        }
        return 0;
    }

    public void ImportAbilities()
    {
        throw new NotImplementedException();
    }

    public void ImportUnits()
    {
        throw new NotImplementedException();
    }

    public void ImportUpgrades()
    {
        throw new NotImplementedException();
    }
}
