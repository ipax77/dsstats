namespace dsstats.shared.Interfaces;

public interface IStatsService
{
    Task<T> GetStatsAsync<T>(StatsType type, StatsRequest request, CancellationToken token = default) where T : IStatsResponse;
}
