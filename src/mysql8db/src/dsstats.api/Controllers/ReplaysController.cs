using dsstats.shared;
using dsstats.shared8.Interfaces;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace dsstats.api.Controllers;

[EnableCors("dsstatsOrigin")]
[ApiController]
[Route("api/v1/[controller]")]
public class ReplaysController(IReplaysService replaysService) : Controller
{
    [HttpPost("getcount")]
    public async Task<ActionResult<int>> GetReplaysCount(ReplaysRequest request, CancellationToken token)
    {
        var result = await replaysService.GetReplaysCount(request, token);
        return Ok(result);
    }

    [HttpPost("get")]
    public async Task<ActionResult<List<ReplayListDto>>> GetReplays(ReplaysRequest request, CancellationToken token)
    {
        var result = await replaysService.GetReplays(request, token);
        return Ok(result);
    }
}

