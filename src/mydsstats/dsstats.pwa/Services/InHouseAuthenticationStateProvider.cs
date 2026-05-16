using System.Security.Claims;
using dsstats.shared.InHouse;
using Microsoft.AspNetCore.Components.Authorization;

namespace dsstats.pwa.Services;

public sealed class InHouseAuthenticationStateProvider : AuthenticationStateProvider, IDisposable
{
    private static readonly ClaimsPrincipal Anonymous = new(new ClaimsIdentity());
    private readonly InHouseAuthClient authClient;

    public InHouseAuthenticationStateProvider(InHouseAuthClient authClient)
    {
        this.authClient = authClient;
        this.authClient.AuthenticationChanged += OnAuthenticationChanged;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        await authClient.InitializeAsync();

        return new AuthenticationState(authClient.IsAuthenticated && authClient.User is not null
            ? CreatePrincipal(authClient.User)
            : Anonymous);
    }

    private void OnAuthenticationChanged()
        => NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());

    private static ClaimsPrincipal CreatePrincipal(InHouseUserDto user)
    {
        List<Claim> claims = new()
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Name, user.DisplayName),
        };
        if (user.IsAdmin)
        {
            claims.Add(new Claim(ClaimTypes.Role, InHouseRoles.Admin));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "InHouse"));
    }

    public void Dispose()
        => authClient.AuthenticationChanged -= OnAuthenticationChanged;
}
