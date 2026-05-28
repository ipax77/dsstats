using System.Collections.Concurrent;
using System.Net.Http.Json;
using dsstats.shared;
using dsstats.shared.Interfaces;

namespace dsstats.apiServices;

public sealed class UnitLifeCostService(IHttpClientFactory httpClientFactory) : IUnitLifeCostService
{
    private static readonly IReadOnlyDictionary<string, DsUnitLifeCostDto> Empty = new Dictionary<string, DsUnitLifeCostDto>();
    private readonly ConcurrentDictionary<Commander, IReadOnlyDictionary<string, DsUnitLifeCostDto>> cache = [];
    private readonly HttpClient httpClient = httpClientFactory.CreateClient("api");

    public async Task<IReadOnlyDictionary<string, DsUnitLifeCostDto>> GetUnitLifeCosts(
        Commander commander,
        CancellationToken token = default)
    {
        if (cache.TryGetValue(commander, out var cached))
        {
            return cached;
        }

        try
        {
            var response = await httpClient.GetFromJsonAsync<Dictionary<string, DsUnitLifeCostDto>>(
                $"api10/Builds/units/life-cost?commander={commander}",
                token);

            var result = response ?? Empty;
            cache[commander] = result;
            return result;
        }
        catch (Exception)
        {
            return Empty;
        }
    }
}
