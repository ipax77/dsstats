using dsstats.shared;
using dsstats.shared.Interfaces;
using dsstats.shared.Units;
using Microsoft.AspNetCore.Mvc;

namespace dsstats.api.Controllers;

[ApiController]
[Route("api10/[controller]")]
public class BuildsController(IBuildsService buildsService) : Controller
{

    [HttpPost]
    public async Task<IActionResult> GetBuilds([FromBody] BuildsRequest request, CancellationToken token = default)
    {
        var response = await buildsService.GetBuildResponse(request, token);
        return Ok(response);
    }

    [HttpPost("units")]
    public async Task<ActionResult<List<DsUnitListDto>>> GetUnits(DsUnitsRequest request)
    {
        var units = await buildsService.GetUnits(request);
        return Ok(units);
    }

    [HttpGet("unit/{id:int}")]
    public async Task<ActionResult<DsUnitDto>> GetUnit(int id)
    {
        var unit = await buildsService.GetUnit(id);
        return Ok(unit);
    }
}
