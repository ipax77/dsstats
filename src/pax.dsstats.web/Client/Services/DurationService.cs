using pax.dsstats.shared.Interfaces;
using pax.dsstats.shared;
using System.Text.Json;
using System.Net.Http.Json;

namespace pax.dsstats.web.Client.Services;

public class DurationService : IDurationService
{
    private readonly string statsController = "api/v6/Stats";
    private readonly HttpClient httpClient;
    private readonly ILogger<DurationService> logger;

    public DurationService(HttpClient httpClient, ILogger<DurationService> logger)
    {
        this.httpClient = httpClient;
        this.logger = logger;
    }

    public async Task<DurationResponse> GetDuration(DurationRequest request, CancellationToken token = default)
    {
        try 
        {
            var response = await httpClient.PostAsJsonAsync($"{statsController}/duration", request, token);
            response.EnsureSuccessStatusCode();
            JsonSerializerOptions? options = null;
            var content = await response.Content.ReadFromJsonAsync<DurationResponse>(options, token);
            return content ?? new();
        }
        catch(Exception e)
        {
            logger.LogError($"failed getting duration: {e.Message}");
        }
        return new();
    }
}