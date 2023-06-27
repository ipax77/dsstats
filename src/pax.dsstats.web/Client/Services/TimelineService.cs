using Microsoft.Extensions.Logging;
using pax.dsstats.shared;
using pax.dsstats.shared.Interfaces;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace pax.dsstats.web.Client.Services;

public class TimelineService : ITimelineService
{
    public TimelineService(HttpClient httpClient, ILogger<TimelineService> logger)
    {
        this.httpClient = httpClient;
        this.logger = logger;
    }

    private readonly string statsController = "api/v6/Stats";
    private readonly HttpClient httpClient;
    private readonly ILogger<TimelineService> logger;

    public async Task<TimelineResponse> GetTimeline(TimelineRequest request, CancellationToken token = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync($"{statsController}/timeline", request, token);
            response.EnsureSuccessStatusCode();
            JsonSerializerOptions? options = null;
            var content = await response.Content.ReadFromJsonAsync<TimelineResponse>(options, token);
            return content ?? new();
        }
        catch (Exception e)
        {
            logger.LogError($"failed getting duration: {e.Message}");
        }
        return new();
    }
}
