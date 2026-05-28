namespace dsstats.shared.Interfaces;

public interface IUnitLifeCostService
{
    Task<IReadOnlyDictionary<string, DsUnitLifeCostDto>> GetUnitLifeCosts(
        Commander commander,
        CancellationToken token = default);
}
