using dsstats.shared;
using dsstats.shared.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace dsstats.api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class CmdrInfoController : Controller
{
    private readonly ICmdrInfoService cmdrInfoService;

    public CmdrInfoController(ICmdrInfoService cmdrInfoService)
    {
        this.cmdrInfoService = cmdrInfoService;
    }

    [HttpPost]
    public async Task<ActionResult<List<CmdrPlayerInfo>>> GetCmdrPlayerInfos(CmdrInfoRequest request, CancellationToken token = default)
    {
        return await cmdrInfoService.GetCmdrPlayerInfos(request, token);
    }
}
