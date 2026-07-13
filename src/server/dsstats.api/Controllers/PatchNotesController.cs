using dsstats.shared;
using dsstats.shared.Interfaces;
using dsstats.shared.PatchNotes;
using Microsoft.AspNetCore.Mvc;

namespace dsstats.api.Controllers;

[ApiController]
[Route("api10/[controller]")]
public sealed class PatchNotesController(IPatchNotesService patchNotesService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<PatchNotesPage>> GetPatchNotes(
        [FromBody] PatchNotesRequest request,
        CancellationToken token = default)
    {
        return Ok(await patchNotesService.GetPatchNotes(request, token));
    }

    [HttpGet("units")]
    public async Task<ActionResult<IReadOnlyList<string>>> GetUnitNames(
        [FromQuery] Commander commander = Commander.None,
        CancellationToken token = default)
    {
        return Ok(await patchNotesService.GetUnitNames(commander, token));
    }
}
