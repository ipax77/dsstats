using pax.dsstats.shared.Services;

namespace pax.dsstats.shared.Interfaces;

public interface IDsUpdateService
{
    Task<List<DsUpdateInfo>> GetDsUpdates(TimePeriod timePeriod, CancellationToken token = default);
    void SeedDsUpdates();
}