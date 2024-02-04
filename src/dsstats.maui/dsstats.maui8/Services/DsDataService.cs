using dsstats.shared;
using dsstats.shared.Interfaces;

namespace dsstats.maui8.Services;

public class DsDataService([FromKeyedServices("local")] IDsDataService localDsDataService,
                           [FromKeyedServices("remote")] IDsDataService remoteDsDataService,
                           IRemoteToggleService remoteToggleService) : IDsDataService
{
    public async Task<SpawnInfo> GetDsUnitSpawnInfo(SpawnDto spawn, Commander cmdr)
    {
        if (remoteToggleService.FromServer)
        {
            return await remoteDsDataService.GetDsUnitSpawnInfo(spawn , cmdr);
        }
        else
        {
            return await localDsDataService.GetDsUnitSpawnInfo(spawn , cmdr);
        }
    }

    public async Task<SpawnInfo> GetSpawnInfo(SpawnRequest request)
    {
        if (remoteToggleService.FromServer)
        {
            return await remoteDsDataService.GetSpawnInfo(request);
        }
        else
        {
            return await localDsDataService.GetSpawnInfo(request);
        }
    }

    public Task<DsUnitDto?> GetUnitDetails(UnitDetailRequest request, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    public Task<int> GetUnitId(UnitDetailRequest request)
    {
        throw new NotImplementedException();
    }

    public Task<List<DsUnitListDto>> GetUnitsList(UnitRequest request, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    public Task<int> GetUnitsListCount(UnitRequest request, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    public async Task SetBuildResponseLifeAndCost(BuildResponse buildResponse, Commander cmdr)
    {
        if (remoteToggleService.FromServer)
        {
            await remoteDsDataService.SetBuildResponseLifeAndCost(buildResponse, cmdr);
        }
        else
        {
            await localDsDataService.SetBuildResponseLifeAndCost(buildResponse, cmdr);
        }
    }
}
