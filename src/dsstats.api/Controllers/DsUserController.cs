using dsstats.auth.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace dsstats.api.Controllers;

[Authorize]
[EnableRateLimiting("fixed")]
[ApiController]
[Route("api8/v1/[controller]")]
public class DsUserController(UserRepository userRepository) : Controller
{
    [HttpGet]
    [Route("isinrole/{role}")]
    public ActionResult<bool> IsUserInRole(string role)
    {
        return Ok(User.IsInRole(role));
    }

    [HttpGet]
    [Route("requestnewemail/{newemail}")]
    public async Task<ActionResult<bool>> RequestNewEmail(string newemail)
    {
        var result = await userRepository.SendEmailChangeToken(User, newemail);
        return Ok(result);
    }

    [HttpGet]
    [Route("changeemail/{newemail}/{token}")]
    public async Task<ActionResult<bool>> ChangeEmail(string newemail, string token)
    {
        var result = await userRepository.ChangeEmail(User, newemail, token);
        return Ok(result);
    }

    [HttpGet]
    [Route("changename/{newname}")]
    public async Task<ActionResult<bool>> ChangeName(string newname)
    {
        var result = await userRepository.ChangeName(User, newname);
        return Ok(result);
    }

    [HttpGet]
    [Route("delete")]
    public async Task<ActionResult<bool>> DeleteUser()
    {
        var result = await userRepository.DeleteUser(User);
        return Ok(result);
    }
}
