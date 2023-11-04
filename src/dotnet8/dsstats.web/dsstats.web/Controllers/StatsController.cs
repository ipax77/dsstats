using dsstats.shared;
using dsstats.shared.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace dsstats.web.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class StatsController : ControllerBase
{
    private readonly IWinrateService winrateService;
    private readonly ILogger<StatsController> logger;

    public StatsController(IWinrateService winrateService, ILogger<StatsController> logger)
    {
        this.winrateService = winrateService;
        this.logger = logger;
    }

    [HttpPost]
    [Route("winrate")]
    public async Task<ActionResult<WinrateResponse>> GetWinrate(WinrateRequest request, CancellationToken token)
    {
        logger.LogInformation("getting winrate from controller - {date}", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
        return await winrateService.GetWinrate(request, token);
    }
}
