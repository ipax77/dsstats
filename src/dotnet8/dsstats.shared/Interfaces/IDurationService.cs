namespace dsstats.shared.Interfaces;

public interface IDurationService
{
    Task<DurationResponse> GetDuration(StatsRequest request, CancellationToken token = default);
}
