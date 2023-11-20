namespace dsstats.shared.Interfaces;

public interface IDamageService
{
    Task<DamageResponse> GetDamage(StatsRequest request, CancellationToken token);
}
