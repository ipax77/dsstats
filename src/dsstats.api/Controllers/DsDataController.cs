using dsstats.shared;
using dsstats.shared.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace dsstats.api.Controllers;

[ApiController]
[Route("api8/v1/[controller]")]
public class DsDataController(IDsDataService dsDataService) : Controller
{
    [HttpPost]
    [Route("getspawninfo")]
    public async Task<ActionResult<SpawnInfo>> GetSpawnInfo(SpawnRequest request)
    {
        return await dsDataService.GetSpawnInfo(request);
    }

    [HttpPost]
    [Route("getunitid")]
    public async Task<ActionResult<int>> GetUnitId(UnitDetailRequest request)
    {
        return await dsDataService.GetUnitId(request);
    }

    [HttpPost]
    [Route("getunitdetails")]
    public async Task<ActionResult<DsUnitDto?>> GetUnitDetails(UnitDetailRequest request, CancellationToken token = default)
    {
        return await dsDataService.GetUnitDetails(request, token);
    }

    [HttpPost]
    [Route("getunitscount")]
    public async Task<int> GetUnitsCount(UnitRequest request, CancellationToken token = default)
    {
        return await dsDataService.GetUnitsListCount(request, token);
    }

    [HttpPost]
    [Route("getunits")]
    public async Task<ActionResult<List<DsUnitListDto>>> GetUnits(UnitRequest request, CancellationToken token)
    {
        return await dsDataService.GetUnitsList(request, token);
    }
}
