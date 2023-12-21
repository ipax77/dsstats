using dsstats.shared;
using dsstats.shared.Interfaces;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace dsstats.apiServices;

public class UnitmapService(HttpClient httpClient, ILogger<UnitmapService> logger) : IUnitmapService
{
    private readonly string buildController = "api8/v1/builds";

    public async Task<Unitmap> GetUnitMap(UnitmapRequest request)
    {
        try
        {
            var result = await httpClient.PostAsJsonAsync($"{buildController}/unitmap", request);
            result.EnsureSuccessStatusCode();

            var unitmap = await result.Content.ReadFromJsonAsync<Unitmap>();
            return unitmap ?? new();
        }
        catch (Exception ex)
        {
            logger.LogError("failed getting unitmap: {error}", ex.Message);
        }
        return new();
    }
}
