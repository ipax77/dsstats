using dsstats.shared;

namespace dsstats.shared.Interfaces;

public interface IDsDataService
{
    Task<DsUnitDto?> GetUnitDetails(UnitDetailRequest request, CancellationToken token = default);
    Task<List<DsUnitListDto>> GetUnitsList(UnitRequest request, CancellationToken token = default);
    Task<int> GetUnitsListCount(UnitRequest request, CancellationToken token = default);
    void ImportAbilities();
    void ImportUnits();
    void ImportUpgrades();
}