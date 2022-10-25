using pax.dsstats.shared;

namespace pax.dsstats.dbng.Repositories
{
    public interface IStatsRepository
    {
        Task<IStatsResponse> GetStats(StatsRequest request, CancellationToken token = default);

    }
}