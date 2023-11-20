namespace dsstats.shared.Interfaces;

public interface ITimelineService
{
    Task<TimelineResponse> GetTimeline(StatsRequest request, CancellationToken token = default);
    Task<List<DsUpdateInfo>> GetDsUpdates(TimePeriod timePeriod, CancellationToken token = default);
}
