using dsstats.shared.Units;

namespace dsstats.shared.Interfaces;

public interface IBuildsService
{
    Task<BuildsResponse> GetBuildResponse(BuildsRequest request, CancellationToken token = default);
    Task<List<DsUnitListDto>> GetUnits(DsUnitsRequest request);
    Task<DsUnitDto> GetUnit(int dsUnitId);
}
