using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace dsstats.api.Controllers;

[Authorize]
[EnableRateLimiting("fixed")]
[ApiController]
[Route("api8/v1/[controller]")]
public class DsUserController : Controller
{
    [HttpGet]
    [Route("isinrole/{role}")]
    public ActionResult<bool> IsUserInRole(string role)
    {
        return Ok(User.IsInRole(role));
    }
}
