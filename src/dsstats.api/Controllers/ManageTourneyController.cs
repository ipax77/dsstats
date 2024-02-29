using dsstats.auth.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace dsstats.api.Controllers;


[ApiController]
[Route("api8/v1/[controller]")]
[Authorize]
public class ManageTourneyController
{
    [HttpGet]
    [Route("test1")]
    public string Test1()
    {
        return "Und es war Sommer";
    }

    [HttpGet]
    [Route("test2")]
    [AllowAnonymous]
    public string Test2()
    {
        return "Und es war Winter";
    }

    [HttpGet]
    [Route("test3")]
    [Authorize(Policy = DsPolicy.TourneyManager)]
    public string Test3()
    {
        return "Und es war Herbst";
    }
}
