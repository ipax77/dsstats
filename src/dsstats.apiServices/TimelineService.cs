using dsstats.shared;
using dsstats.shared.Interfaces;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace dsstats.apiServices;

public class TimelineService : ITimelineService
{
    private readonly HttpClient httpClient;
    private readonly ILogger<TimelineService> logger;
    private readonly string statsController = "api8/v1/stats";

    public TimelineService(HttpClient httpClient, ILogger<TimelineService> logger)
    {
        this.httpClient = httpClient;
        this.logger = logger;
    }

    public async Task<List<DsUpdateInfo>> GetDsUpdates(TimePeriod timePeriod, CancellationToken token = default)
    {
        try
        {
            return await httpClient
                .GetFromJsonAsync<List<DsUpdateInfo>>($"{statsController}/dsupdates/{(int)timePeriod}", token)
                ?? new();
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            logger.LogError("failed getting dsdupdates: {error}", ex.Message);
        }
        return new();
    }

    public async Task<TimelineResponse> GetTimeline(StatsRequest request, CancellationToken token = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync($"{statsController}/timeline", request, token);
            response.EnsureSuccessStatusCode();
            var data = await response.Content.ReadFromJsonAsync<TimelineResponse>();

            if (data == null)
            {
                logger.LogError($"failed getting timeline");
            }
            else
            {
                return data;
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            logger.LogError("failed getting timeline: {error}", ex.Message);
        }
        return new();
    }
}
