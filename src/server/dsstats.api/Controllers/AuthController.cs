using System.Security.Claims;
using dsstats.api.InHouse;
using dsstats.shared.InHouse;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace dsstats.api.Controllers;

[ApiController]
[Route("api10/auth")]
public sealed class AuthController(IInHouseAuthService authService) : ControllerBase
{
    [HttpPost("register/options")]
    public async Task<ActionResult<InHouseAuthOptionsResponse>> RegisterOptions(InHouseRegisterOptionsRequest request, CancellationToken token)
        => await ExecuteAsync(() => authService.BeginRegistrationAsync(request, token));

    [HttpPost("register/complete")]
    public async Task<ActionResult<InHouseSessionDto>> RegisterComplete(InHouseRegisterCompleteRequest request, CancellationToken token)
        => await ExecuteAsync(() => authService.CompleteRegistrationAsync(request, token));

    [HttpPost("login/options")]
    public async Task<ActionResult<InHouseAuthOptionsResponse>> LoginOptions(InHouseLoginOptionsRequest request, CancellationToken token)
        => await ExecuteAsync(() => authService.BeginLoginAsync(request, token));

    [HttpPost("login/complete")]
    public async Task<ActionResult<InHouseSessionDto>> LoginComplete(InHouseLoginCompleteRequest request, CancellationToken token)
        => await ExecuteAsync(() => authService.CompleteLoginAsync(request, token));

    [HttpPost("refresh")]
    public async Task<ActionResult<InHouseSessionDto>> Refresh(InHouseRefreshRequest request, CancellationToken token)
        => await ExecuteAsync(() => authService.RefreshAsync(request, token));

    [Authorize(AuthenticationSchemes = Authentication.InHouseBearerAuthenticationHandler.SchemeName)]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(InHouseRefreshRequest request, CancellationToken token)
    {
        await authService.LogoutAsync(GetBearerToken(), request, token);
        return NoContent();
    }

    [Authorize(AuthenticationSchemes = Authentication.InHouseBearerAuthenticationHandler.SchemeName)]
    [HttpGet("me")]
    public async Task<ActionResult<InHouseUserDto>> Me(CancellationToken token)
    {
        if (!int.TryParse(User.FindFirstValue(InHouseClaims.UserId), out var userId))
        {
            return Unauthorized();
        }

        var user = await authService.GetCurrentUserAsync(userId, token);
        return user is null ? Unauthorized() : user;
    }

    [Authorize(AuthenticationSchemes = Authentication.InHouseBearerAuthenticationHandler.SchemeName)]
    [HttpDelete("me")]
    public async Task<IActionResult> DeleteMe(CancellationToken token)
    {
        if (!int.TryParse(User.FindFirstValue(InHouseClaims.UserId), out var userId))
        {
            return Unauthorized();
        }

        await authService.DeleteAccountAsync(userId, token);
        return NoContent();
    }

    [Authorize(AuthenticationSchemes = Authentication.InHouseBearerAuthenticationHandler.SchemeName)]
    [HttpPost("profiles")]
    public async Task<ActionResult<InHouseUserDto>> AddProfile(InHouseProfileDto profile, CancellationToken token)
    {
        if (!int.TryParse(User.FindFirstValue(InHouseClaims.UserId), out var userId))
        {
            return Unauthorized();
        }

        return await ExecuteAsync(() => authService.AddProfileAsync(userId, profile, token));
    }

    [Authorize(AuthenticationSchemes = Authentication.InHouseBearerAuthenticationHandler.SchemeName)]
    [HttpPost("profiles/remove")]
    public async Task<ActionResult<InHouseUserDto>> RemoveProfile(InHouseProfileDto profile, CancellationToken token)
    {
        if (!int.TryParse(User.FindFirstValue(InHouseClaims.UserId), out var userId))
        {
            return Unauthorized();
        }

        return await ExecuteAsync(() => authService.RemoveProfileAsync(userId, profile, token));
    }

    [Authorize(AuthenticationSchemes = Authentication.InHouseBearerAuthenticationHandler.SchemeName)]
    [HttpPost("passkeys/remove")]
    public async Task<ActionResult<InHouseUserDto>> RemovePasskey(InHouseRemovePasskeyRequest request, CancellationToken token)
    {
        if (!int.TryParse(User.FindFirstValue(InHouseClaims.UserId), out var userId))
        {
            return Unauthorized();
        }

        return await ExecuteAsync(() => authService.RemovePasskeyAsync(userId, request.PasskeyId, token));
    }

    [Authorize(AuthenticationSchemes = Authentication.InHouseBearerAuthenticationHandler.SchemeName)]
    [EnableRateLimiting("inhouse-device-link-create")]
    [HttpPost("device-link/code")]
    public async Task<ActionResult<InHouseDeviceLinkOptionsResponse>> DeviceLinkCode(CancellationToken token)
    {
        if (!int.TryParse(User.FindFirstValue(InHouseClaims.UserId), out var userId))
        {
            return Unauthorized();
        }

        return await ExecuteAsync(() => authService.CreateDeviceLinkCodeAsync(userId, token));
    }

    [EnableRateLimiting("inhouse-device-link-attempt")]
    [HttpPost("device-link/options")]
    public async Task<ActionResult<object>> DeviceLinkOptions(InHouseDeviceLinkOptionsRequest request, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(request.Code)
            && int.TryParse(User.FindFirstValue(InHouseClaims.UserId), out var userId))
        {
            return await ExecuteObjectAsync(() => authService.CreateDeviceLinkCodeAsync(userId, token));
        }

        return await ExecuteObjectAsync(() => authService.BeginDeviceLinkAsync(request, token));
    }

    [EnableRateLimiting("inhouse-device-link-attempt")]
    [HttpPost("device-link/complete")]
    public async Task<ActionResult<InHouseSessionDto>> DeviceLinkComplete(InHouseDeviceLinkCompleteRequest request, CancellationToken token)
        => await ExecuteAsync(() => authService.CompleteDeviceLinkAsync(request, token));

    private async Task<ActionResult<T>> ExecuteAsync<T>(Func<Task<T>> action)
    {
        try
        {
            return await action();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private async Task<ActionResult<object>> ExecuteObjectAsync<T>(Func<Task<T>> action)
    {
        try
        {
            var value = await action();
            return Ok(value);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private string? GetBearerToken()
    {
        var authorization = Request.Headers.Authorization.ToString();
        return authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? authorization["Bearer ".Length..].Trim()
            : null;
    }
}
