
namespace dsstats.shared.Interfaces;

public interface ICountService
{
    Task<CountResponse> GetCount(StatsRequest request, CancellationToken token = default);
}
