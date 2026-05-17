using dsstats.shared.Units;

namespace dsstats.shared.Interfaces;

public interface IBuildsService
{
    Task<BuildsResponse> GetBuildResponse(BuildsRequest request, CancellationToken token = default);
    Task<List<BuildUpgradeTimingDto>> GetUpgradeTimings(BuildsRequest request, CancellationToken token = default);
    Task<List<BuildGasTimingDto>> GetGasTimings(BuildsRequest request, CancellationToken token = default);
    Task<List<DsUnitListDto>> GetUnits(DsUnitsRequest request);
    Task<DsUnitDto> GetUnit(int dsUnitId);
}
