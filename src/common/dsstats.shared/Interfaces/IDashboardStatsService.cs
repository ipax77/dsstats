
namespace dsstats.shared.Interfaces;

public interface IDashboardStatsService
{
    Task<DashboardStatsResponse> GetDashboardStatsAsync(CancellationToken token = default);
}