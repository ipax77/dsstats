using Blazored.LocalStorage;
using dsstats.shared.Auth;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace dsstats.web.Client.Services;

public class DsAuthStateProvider(IAuthService authService, ILocalStorageService localStorage, ILogger<DsAuthStateProvider> logger) : AuthenticationStateProvider
{
    private ClaimsPrincipal currentUser = new ClaimsPrincipal(new ClaimsIdentity());
    private TokenInfo? tokenInfo = null;

    public override Task<AuthenticationState> GetAuthenticationStateAsync() =>
        Task.FromResult(new AuthenticationState(currentUser));

    public Task LoginAsync(LoginPayload login)
    {
        var loginTask = LogInAsyncCore();
        NotifyAuthenticationStateChanged(loginTask);

        return loginTask;

        async Task<AuthenticationState> LogInAsyncCore()
        {
            var user = await LoginWithDsApiAsync(login);
            currentUser = user;

            return new AuthenticationState(currentUser);
        }
    }

    private async Task<ClaimsPrincipal> LoginWithDsApiAsync(LoginPayload login)
    {
        var result = await authService.Login(login);

        if (result is LoginSucessResponse successResponse)
        {
            tokenInfo = successResponse.TokenInfo;

            List<Claim> claims = new()
            {
                new Claim(ClaimTypes.Name, login.Email),
                new Claim(ClaimTypes.Email, login.Email)
            };

            var identity = new ClaimsIdentity(claims, "custom", ClaimsIdentity.DefaultNameClaimType, ClaimsIdentity.DefaultRoleClaimType);

            try
            {
                await localStorage.SetItemAsync("dsauth", tokenInfo);
            }
            catch (Exception ex)
            {
                logger.LogError("failed storing dsauth in local storage: {error}", ex.Message);
            }

            return new ClaimsPrincipal(identity);
        }
        else
        {
            return currentUser;
        }
    }

    public Task Logout()
    {
        var logoutTask = LogoutAsyncCore();
        NotifyAuthenticationStateChanged(logoutTask);

        return logoutTask;

        async Task<AuthenticationState> LogoutAsyncCore()
        {
            currentUser = new ClaimsPrincipal(new ClaimsIdentity());
            NotifyAuthenticationStateChanged(
                Task.FromResult(new AuthenticationState(currentUser)));

            try
            {
                if (await localStorage.ContainKeyAsync("dsauth"))
                {
                    await localStorage.RemoveItemAsync("dsauth");
                }
            }
            catch (Exception ex)
            {
                logger.LogError("failed clearing localstorage: {error}", ex.Message);
            }

            return new AuthenticationState(currentUser);
        }
    }
}
