using Microsoft.AspNetCore.Components.Authorization;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;

namespace dsstats.web.Client.Services;

public class ExternalAuthStateProvider(HttpClient httpClient, IHttpClientFactory httpClientFactory, ILogger<ExternalAuthStateProvider> logger) : AuthenticationStateProvider
{
    private ClaimsPrincipal currentUser = new ClaimsPrincipal(new ClaimsIdentity());
    private TokenResponse? tokenResponse = null;

    public override Task<AuthenticationState> GetAuthenticationStateAsync() =>
        Task.FromResult(new AuthenticationState(currentUser));

    public Task LogInAsync(string email, string password)
    {
        var loginTask = LogInAsyncCore();
        NotifyAuthenticationStateChanged(loginTask);

        return loginTask;

        async Task<AuthenticationState> LogInAsyncCore()
        {
            var user = await LoginWithExternalProviderAsync(email, password);
            currentUser = user;

            return new AuthenticationState(currentUser);
        }
    }

    private async Task<ClaimsPrincipal> LoginWithExternalProviderAsync(string email, string password)
    {
        try
        {
            var result = await httpClient.PostAsJsonAsync("account/login", new { email = email, password = password });
            result.EnsureSuccessStatusCode();
            tokenResponse = await result.Content.ReadFromJsonAsync<TokenResponse>();
            ArgumentNullException.ThrowIfNull(tokenResponse);
        }
        catch (Exception ex)
        {
            logger.LogError("login failed: {error}", ex.Message);
            // todo: error response
            return currentUser;
        }

        List<Claim> claims = new()
        {
            new Claim(ClaimTypes.Name, email),
            new Claim(ClaimTypes.Email, email)
        };

        var identity = new ClaimsIdentity(claims, "custom", ClaimsIdentity.DefaultNameClaimType, ClaimsIdentity.DefaultRoleClaimType);

        return new ClaimsPrincipal(identity);
    }

    private List<Claim>? GetClaimsFromToken(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadJwtToken(token);
            return jsonToken?.Claims
                .Select(claim => new Claim(claim.Type, claim.Value))
                .ToList();
        }
        catch (Exception ex)
        {
            logger.LogError("failed getting token claims: {error}", ex.Message);
        }
        return null;
    }

    public async Task RefreshToken()
    {
        if (tokenResponse is null)
        {
            return;
        }

        try
        {
            var result = await httpClient.PostAsJsonAsync("account/refresh", new { refreshToken = tokenResponse.RefreshToken });
            result.EnsureSuccessStatusCode();
            tokenResponse = await result.Content.ReadFromJsonAsync<TokenResponse>();
        }
        catch (Exception ex)
        {
            logger.LogError("Token refresh failed: {errpr}", ex.Message);
        }
    }

    public async void Logout()
    {
        try
        {
            var result = await httpClient.PostAsJsonAsync("account/logout", new object());
            result.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            logger.LogError("Logout failed: {error}", ex.Message);
        }
        finally
        {
            currentUser = new ClaimsPrincipal(new ClaimsIdentity());
            NotifyAuthenticationStateChanged(
                Task.FromResult(new AuthenticationState(currentUser)));
        }
    }

    public HttpClient GetApiHttpClient()
    {
        var httpClient = httpClientFactory.CreateClient("AuthAPI");

        if (tokenResponse != null)
        {
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenResponse.AccessToken);
        }

        return httpClient;
    }
}

public record TokenResponse
{
    public string TokenType { get; init; } = string.Empty;
    public string AccessToken { get; init; } = string.Empty;
    public int ExpiresIn { get; init; }
    public string RefreshToken { get; init; } = string.Empty;
}