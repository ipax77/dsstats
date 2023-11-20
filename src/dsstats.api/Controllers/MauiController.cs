using dsstats.db8services;
using dsstats.shared.Maui;
using Microsoft.AspNetCore.Mvc;

namespace dsstats.api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class MauiController(MauiService mauiService) : Controller
{


    [HttpPost]
    public async Task<ActionResult<MauiRatingResponse>> GetMauiRatings(MauiRatingRequest request, CancellationToken token = default)
    {
        return await mauiService.GetMauiRatings(request, token);
    }
}
