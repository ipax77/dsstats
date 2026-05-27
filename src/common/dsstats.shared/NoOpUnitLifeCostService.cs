using dsstats.shared.Interfaces;

namespace dsstats.shared;

public sealed class NoOpUnitLifeCostService : IUnitLifeCostService
{
    private static readonly IReadOnlyDictionary<string, DsUnitLifeCostDto> Empty = new Dictionary<string, DsUnitLifeCostDto>();

    public Task<IReadOnlyDictionary<string, DsUnitLifeCostDto>> GetUnitLifeCosts(
        Commander commander,
        CancellationToken token = default)
    {
        return Task.FromResult(Empty);
    }
}
