using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using dsstats.indexedDb.Services;
using dsstats.shared.InHouse;
using Microsoft.JSInterop;

namespace dsstats.pwa.Services;

public sealed class InHouseAuthClient(
    IHttpClientFactory httpClientFactory,
    IndexedDbService indexedDb,
    IJSRuntime jsRuntime)
{
    private readonly HttpClient http = httpClientFactory.CreateClient("InHouseApi");
    private InHouseSessionDto? session;
    private bool initialized;

    public Uri ApiBaseAddress => http.BaseAddress ?? new Uri("https://dsstats.pax77.org");
    public InHouseUserDto? User => session?.User;
    public bool IsAuthenticated => session is not null && session.RefreshExpiresAt > DateTime.UtcNow;
    public event Action? AuthenticationChanged;

    public async Task InitializeAsync()
    {
        if (initialized)
        {
            return;
        }

        initialized = true;
        session = await indexedDb.GetInHouseSession();
        if (session is null)
        {
            return;
        }

        if (session.ExpiresAt <= DateTime.UtcNow.AddMinutes(1))
        {
            await TryRefreshAsync();
        }

        if (session is not null)
        {
            try
            {
                session.User = await SendAuthorizedAsync<InHouseUserDto>(HttpMethod.Get, "api10/auth/me");
                await indexedDb.SaveInHouseSession(session);
                NotifyAuthenticationChanged();
            }
            catch
            {
                await ClearSessionAsync();
            }
        }
    }

    public async Task<bool> IsPasskeySupportedAsync()
        => await jsRuntime.InvokeAsync<bool>("dsstatsInHousePasskeys.isSupported");

    public async Task<InHouseSessionDto> RegisterAsync(string displayName, InHouseProfileDto profile)
    {
        var options = await PostAsync<InHouseRegisterOptionsRequest, InHouseAuthOptionsResponse>(
            "api10/auth/register/options",
            new InHouseRegisterOptionsRequest { DisplayName = displayName, Profile = profile });

        var credential = await jsRuntime.InvokeAsync<JsonElement>("dsstatsInHousePasskeys.create", options.Options);
        var completed = await PostAsync<InHouseRegisterCompleteRequest, InHouseSessionDto>(
            "api10/auth/register/complete",
            new InHouseRegisterCompleteRequest
            {
                ChallengeId = options.ChallengeId,
                DeviceName = GetDeviceName(),
                Credential = credential,
            });

        await SaveSessionAsync(completed);
        return completed;
    }

    public async Task<InHouseSessionDto> LoginAsync()
    {
        var options = await PostAsync<InHouseLoginOptionsRequest, InHouseAuthOptionsResponse>(
            "api10/auth/login/options",
            new InHouseLoginOptionsRequest());

        var credential = await jsRuntime.InvokeAsync<JsonElement>("dsstatsInHousePasskeys.get", options.Options);
        var completed = await PostAsync<InHouseLoginCompleteRequest, InHouseSessionDto>(
            "api10/auth/login/complete",
            new InHouseLoginCompleteRequest
            {
                ChallengeId = options.ChallengeId,
                Credential = credential,
            });

        await SaveSessionAsync(completed);
        return completed;
    }

    public async Task<InHouseDeviceLinkOptionsResponse> CreateDeviceLinkCodeAsync()
        => await PostAuthorizedAsync<InHouseDeviceLinkOptionsRequest, InHouseDeviceLinkOptionsResponse>(
            "api10/auth/device-link/code",
            new InHouseDeviceLinkOptionsRequest());

    public async Task RefreshUserAsync()
    {
        var user = await SendAuthorizedAsync<InHouseUserDto>(HttpMethod.Get, "api10/auth/me");
        await UpdateUserAsync(user);
    }

    public async Task<InHouseUserDto> AddProfileAsync(InHouseProfileDto profile)
    {
        var user = await PostAuthorizedAsync<InHouseProfileDto, InHouseUserDto>("api10/auth/profiles", profile);
        await UpdateUserAsync(user);
        return user;
    }

    public async Task<InHouseUserDto> RemoveProfileAsync(InHouseProfileDto profile)
    {
        var user = await PostAuthorizedAsync<InHouseProfileDto, InHouseUserDto>("api10/auth/profiles/remove", profile);
        await UpdateUserAsync(user);
        return user;
    }

    public async Task RemovePasskeyAsync(int passkeyId)
    {
        await PostAuthorizedAsync<InHouseRemovePasskeyRequest, InHouseUserDto>(
            "api10/auth/passkeys/remove",
            new InHouseRemovePasskeyRequest { PasskeyId = passkeyId });
        await ClearSessionAsync();
    }

    public async Task DeleteAccountAsync()
    {
        await SendAuthorizedNoContentAsync(HttpMethod.Delete, "api10/auth/me");
        await ClearSessionAsync();
    }

    public async Task<InHouseSessionDto> LinkDeviceAsync(string code)
    {
        var options = await PostAsync<InHouseDeviceLinkOptionsRequest, InHouseAuthOptionsResponse>(
            "api10/auth/device-link/options",
            new InHouseDeviceLinkOptionsRequest { Code = code, DeviceName = GetDeviceName() });

        var credential = await jsRuntime.InvokeAsync<JsonElement>("dsstatsInHousePasskeys.create", options.Options);
        var completed = await PostAsync<InHouseDeviceLinkCompleteRequest, InHouseSessionDto>(
            "api10/auth/device-link/complete",
            new InHouseDeviceLinkCompleteRequest
            {
                ChallengeId = options.ChallengeId,
                Credential = credential,
            });

        await SaveSessionAsync(completed);
        return completed;
    }

    public async Task LogoutAsync()
    {
        if (session is not null)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, "api10/auth/logout")
                {
                    Content = JsonContent.Create(new InHouseRefreshRequest { RefreshToken = session.RefreshToken })
                };
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
                await http.SendAsync(request);
            }
            catch
            {
                // Local logout still wins if the server session is already gone.
            }
        }

        await ClearSessionAsync();
    }

    public async Task<string?> GetAccessTokenAsync()
    {
        await InitializeAsync();
        if (session is null)
        {
            return null;
        }

        if (session.ExpiresAt <= DateTime.UtcNow.AddMinutes(1))
        {
            await TryRefreshAsync();
        }

        return session?.AccessToken;
    }

    private async Task<TResponse> SendAuthorizedAsync<TResponse>(HttpMethod method, string url)
    {
        await InitializeAsync();
        if (session is null)
        {
            throw new InvalidOperationException("You are not signed in.");
        }

        if (session.ExpiresAt <= DateTime.UtcNow.AddMinutes(1))
        {
            await TryRefreshAsync();
        }

        using var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session!.AccessToken);
        using var response = await http.SendAsync(request);
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<TResponse>()
            ?? throw new InvalidOperationException("The server returned an empty response.");
    }

    private async Task SendAuthorizedNoContentAsync(HttpMethod method, string url)
    {
        await InitializeAsync();
        if (session is null)
        {
            throw new InvalidOperationException("You are not signed in.");
        }

        if (session.ExpiresAt <= DateTime.UtcNow.AddMinutes(1))
        {
            await TryRefreshAsync();
        }

        using var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session!.AccessToken);
        using var response = await http.SendAsync(request);
        await EnsureSuccessAsync(response);
    }

    private async Task<TResponse> PostAuthorizedAsync<TRequest, TResponse>(string url, TRequest content)
    {
        await InitializeAsync();
        if (session is null)
        {
            throw new InvalidOperationException("You are not signed in.");
        }

        if (session.ExpiresAt <= DateTime.UtcNow.AddMinutes(1))
        {
            await TryRefreshAsync();
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(content)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session!.AccessToken);
        using var response = await http.SendAsync(request);
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<TResponse>()
            ?? throw new InvalidOperationException("The server returned an empty response.");
    }

    private async Task<TResponse> PostAsync<TRequest, TResponse>(string url, TRequest request)
    {
        using var response = await http.PostAsJsonAsync(url, request);
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<TResponse>()
            ?? throw new InvalidOperationException("The server returned an empty response.");
    }

    private async Task TryRefreshAsync()
    {
        if (session is null || session.RefreshExpiresAt <= DateTime.UtcNow)
        {
            await ClearSessionAsync();
            return;
        }

        try
        {
            var refreshed = await PostAsync<InHouseRefreshRequest, InHouseSessionDto>(
                "api10/auth/refresh",
                new InHouseRefreshRequest { RefreshToken = session.RefreshToken });
            await SaveSessionAsync(refreshed);
        }
        catch
        {
            await ClearSessionAsync();
        }
    }

    private async Task SaveSessionAsync(InHouseSessionDto nextSession)
    {
        session = nextSession;
        await indexedDb.SaveInHouseSession(nextSession);
        NotifyAuthenticationChanged();
    }

    private async Task UpdateUserAsync(InHouseUserDto user)
    {
        if (session is null)
        {
            return;
        }

        session.User = user;
        await indexedDb.SaveInHouseSession(session);
        NotifyAuthenticationChanged();
    }

    private async Task ClearSessionAsync()
    {
        session = null;
        await indexedDb.ClearInHouseSession();
        NotifyAuthenticationChanged();
    }

    private void NotifyAuthenticationChanged()
        => AuthenticationChanged?.Invoke();

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var message = await response.Content.ReadAsStringAsync();
        if (!string.IsNullOrWhiteSpace(message))
        {
            try
            {
                using var document = JsonDocument.Parse(message);
                if (document.RootElement.TryGetProperty("message", out var errorMessage))
                {
                    message = errorMessage.GetString();
                }
            }
            catch
            {
                // The raw body is still useful enough.
            }
        }

        throw new InvalidOperationException(string.IsNullOrWhiteSpace(message)
            ? $"Request failed with status {(int)response.StatusCode}."
            : message);
    }

    private static string GetDeviceName()
        => OperatingSystem.IsBrowser() ? "Browser passkey" : Environment.MachineName;
}
