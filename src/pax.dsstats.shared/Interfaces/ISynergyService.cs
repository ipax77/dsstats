namespace pax.dsstats.shared.Interfaces;

public interface ISynergyService
{
    Task<SynergyResponse> GetSynergy(SynergyRequest request, CancellationToken token = default);
}

