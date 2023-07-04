using pax.dsstats.shared;

namespace pax.dsstats.shared.Interfaces;

public interface IDamageService
{
    Task<DamageResponse> GetDamage(DamageRequest request, CancellationToken token = default);
}