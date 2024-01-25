using dsstats.shared;

namespace dsstats.shared.Interfaces;

public interface IDsDataService
{
    Task<SpawnInfo> GetSpawnInfo(SpawnRequest request);
    Task<SpawnInfo> GetDsUnitSpawnInfo(SpawnDto spawn, Commander cmdr);
    Task<int> GetUnitId(UnitDetailRequest request);
    Task<DsUnitDto?> GetUnitDetails(UnitDetailRequest request, CancellationToken token = default);
    Task<List<DsUnitListDto>> GetUnitsList(UnitRequest request, CancellationToken token = default);
    Task<int> GetUnitsListCount(UnitRequest request, CancellationToken token = default);
}