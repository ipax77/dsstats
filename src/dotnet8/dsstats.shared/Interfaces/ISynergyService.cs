namespace dsstats.shared.Interfaces;

public interface ISynergyService
{
    Task<SynergyResponse> GetSynergy(StatsRequest request, CancellationToken token = default);
}
