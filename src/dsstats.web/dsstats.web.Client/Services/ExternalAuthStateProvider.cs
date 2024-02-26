using Blazored.LocalStorage;
using dsstats.shared.Auth;
using Microsoft.AspNetCore.Components.Authorization;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;

namespace dsstats.web.Client.Services;

public class ExternalAuthStateProvider(HttpClient httpClient,
                                       IHttpClientFactory httpClientFactory,
                                       ILocalStorageService localStorage,
                                       ILogger<ExternalAuthStateProvider> logger) : AuthenticationStateProvider
{
    private ClaimsPrincipal currentUser = new ClaimsPrincipal(new ClaimsIdentity());
    private UserInfo? userInfo = null;

    public override Task<AuthenticationState> GetAuthenticationStateAsync() =>
        Task.FromResult(new AuthenticationState(currentUser));

    public ErrorResponse? ErrorResponse { get; set; }

    public async Task<bool> TryLoginFromStore()
    {
        UserInfo? _userInfo = null;
        bool success = false;

        try
        {
            if (await localStorage.ContainKeyAsync("dsuser"))
            {
                _userInfo = await localStorage.GetItemAsync<UserInfo>("dsuser");
                ArgumentNullException.ThrowIfNull(_userInfo);
                userInfo = _userInfo;
            }
        }
        catch (Exception ex)
        {
            logger.LogError("try login failed: {error}", ex.Message);
        }

        if (userInfo is not null)
        {
            List<Claim> claims = new()
                {
                    new Claim(ClaimTypes.Name, userInfo.Username),
                    new Claim(ClaimTypes.Email, userInfo.Username)
                };

            var identity = new ClaimsIdentity(claims, "custom", ClaimsIdentity.DefaultNameClaimType, ClaimsIdentity.DefaultRoleClaimType);
            currentUser = new ClaimsPrincipal(identity);
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
            success = true;
        }

        return success;
    }

    public Task TryLogin()
    {
        try
        {
            var loginTask = LogInFromStoreAsync();
            NotifyAuthenticationStateChanged(loginTask);

            return loginTask;

            async Task<AuthenticationState> LogInFromStoreAsync()
            {
                UserInfo? _userInfo = null;
                try
                {
                    if (await localStorage.ContainKeyAsync("dsuser"))
                    {
                        _userInfo = await localStorage.GetItemAsync<UserInfo>("dsuser");
                        ArgumentNullException.ThrowIfNull(_userInfo);
                        userInfo = _userInfo;
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError("try login failed: {error}", ex.Message);
                }

                if (userInfo is not null)
                {
                    List<Claim> claims = new()
                {
                    new Claim(ClaimTypes.Name, userInfo.Username),
                    new Claim(ClaimTypes.Email, userInfo.Username)
                };

                    var identity = new ClaimsIdentity(claims, "custom", ClaimsIdentity.DefaultNameClaimType, ClaimsIdentity.DefaultRoleClaimType);
                    currentUser = new ClaimsPrincipal(identity);
                }

                return new AuthenticationState(currentUser);
            }
        }
        catch (Exception ex)
        {
            logger.LogError("try login failed: {error}", ex.Message);
        }
        return Task.CompletedTask;
    }

    public Task LogInAsync(string email, string password, bool remember = false)
    {
        var loginTask = LogInAsyncCore();
        NotifyAuthenticationStateChanged(loginTask);

        return loginTask;

        async Task<AuthenticationState> LogInAsyncCore()
        {
            var user = await LoginWithExternalProviderAsync(email, password, remember);
            currentUser = user;

            return new AuthenticationState(currentUser);
        }
    }

    private async Task<ClaimsPrincipal> LoginWithExternalProviderAsync(string email, string password, bool remember)
    {
        TokenResponse? tokenResponse = null;
        HttpResponseMessage? response = null;
        try
        {
            response = await httpClient.PostAsJsonAsync("account/login", new { email = email, password = password });
            response.EnsureSuccessStatusCode();
            tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>();
            ArgumentNullException.ThrowIfNull(tokenResponse);
            ErrorResponse = null;
        }
        catch (Exception ex)
        {
            logger.LogError("login failed: {error}", ex.Message);

            if (response is not null)
            {
                try
                {
                    var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>();
                    ErrorResponse = errorResponse;
                }
                finally { }
            }

            return currentUser;
        }
        finally
        {
            await SetTokenAndUser(tokenResponse, email, remember);
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
        if (userInfo is null)
        {
            return;
        }

        TokenResponse? tokenResponse = null;
        try
        {
            var result = await httpClient.PostAsJsonAsync("account/refresh", new { refreshToken = userInfo.TokenResponse.RefreshToken });
            result.EnsureSuccessStatusCode();
            tokenResponse = await result.Content.ReadFromJsonAsync<TokenResponse>();
            ArgumentNullException.ThrowIfNull(tokenResponse);
        }
        catch (Exception ex)
        {
            logger.LogError("Token refresh failed: {error}", ex.Message);
        }
        await SetTokenAndUser(tokenResponse, userInfo.Username, userInfo.Remember);
    }

    private async Task SetTokenAndUser(TokenResponse? _tokenResponse, string userName, bool remember)
    {
        if (_tokenResponse is null)
        {
            userInfo = null;
            if (await localStorage.ContainKeyAsync("dsuser"))
            {
                await localStorage.RemoveItemAsync("dsuser");
            }
        }
        else
        {
            userInfo = new UserInfo()
            {
                TokenAquired = DateTime.UtcNow,
                Username = userName,
                Remember = remember,
                TokenResponse = _tokenResponse,
            };
            if (remember)
            {
                await localStorage.SetItemAsync("dsuser", userInfo);
            }
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
            await SetTokenAndUser(null, string.Empty, false);
        }
    }

    public bool TryGetApiHttpClient(out HttpClient? httpClient)
    {
        if (userInfo is not null && !userInfo.IsValid())
        {
            RefreshToken().Wait();
        }

        if (userInfo?.IsValid() ?? false)
        {
            httpClient = httpClientFactory.CreateClient("AuthAPI");
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userInfo.TokenResponse.AccessToken);
            return true;
        }
        else
        {
            httpClient = null;
            return false;
        }
    }
}

public record TokenResponse
{
    public string TokenType { get; init; } = string.Empty;
    public string AccessToken { get; init; } = string.Empty;
    public int ExpiresIn { get; init; }
    public string RefreshToken { get; init; } = string.Empty;
}



public record UserInfo
{
    public DateTime TokenAquired { get; init; } = DateTime.UtcNow;
    public string Username { get; init; } = string.Empty;
    public bool Remember { get; init; }
    public TokenResponse TokenResponse { get; init; } = default!;
    public bool IsValid()
    {
        return DateTime.UtcNow - TimeSpan.FromSeconds(TokenResponse.ExpiresIn) < TokenAquired;
    }
}