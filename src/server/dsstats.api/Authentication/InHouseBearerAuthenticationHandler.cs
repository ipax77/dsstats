using System.Security.Claims;
using System.Text.Encodings.Web;
using dsstats.api.InHouse;
using dsstats.shared.InHouse;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace dsstats.api.Authentication;

public sealed class InHouseBearerAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IInHouseAuthService authService)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "InHouseBearer";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var token = GetBearerToken();
        if (string.IsNullOrWhiteSpace(token))
        {
            return AuthenticateResult.NoResult();
        }

        var result = await authService.ValidateAccessTokenAsync(token, Context.RequestAborted);
        if (result is null)
        {
            return AuthenticateResult.Fail("Invalid bearer token.");
        }

        List<Claim> claims = new()
        {
            new Claim(ClaimTypes.NameIdentifier, result.PublicId.ToString()),
            new Claim(ClaimTypes.Name, result.DisplayName),
            new Claim(InHouseClaims.UserId, result.UserId.ToString()),
        };
        if (result.IsAdmin)
        {
            claims.Add(new Claim(ClaimTypes.Role, InHouseRoles.Admin));
        }

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        return AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName));
    }

    private string? GetBearerToken()
    {
        var authorization = Request.Headers.Authorization.ToString();
        if (authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return authorization["Bearer ".Length..].Trim();
        }

        if (Request.Path.StartsWithSegments("/hubs/inhouse")
            && Request.Query.TryGetValue("access_token", out var accessToken))
        {
            return accessToken.ToString();
        }

        return null;
    }
}
