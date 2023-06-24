namespace pax.dsstats.shared.Interfaces;

public interface ITimelineService
{
    Task<TimelineResponse> GetTimeline(TimelineRequest request, CancellationToken token = default);
}