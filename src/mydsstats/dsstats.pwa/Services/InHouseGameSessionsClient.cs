using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using dsstats.shared;
using dsstats.shared.InHouse;

namespace dsstats.pwa.Services;

public sealed class InHouseGameSessionsClient(
    IHttpClientFactory httpClientFactory,
    InHouseAuthClient auth)
{
    private readonly HttpClient http = httpClientFactory.CreateClient("InHouseApi");

    public async Task<InHouseGameSessionDetailDto> CreateSessionAsync(string name)
        => await PostAuthorizedAsync<InHouseCreateGameSessionRequest, InHouseGameSessionDetailDto>(
            "api10/inhouse/sessions",
            new InHouseCreateGameSessionRequest { Name = name });

    public async Task<InHouseGameSessionDetailDto> UploadReplayAsync(Guid sessionId, InHouseParsedReplayDto replay)
        => await PostAuthorizedAsync<InHouseReplayUploadRequest, InHouseGameSessionDetailDto>(
            $"api10/inhouse/sessions/{sessionId}/replays",
            new InHouseReplayUploadRequest
            {
                Replay = replay.Replay,
                Observers = replay.Observers,
            });

    public async Task<InHouseClosedGameSessionsPageDto> GetClosedSessionsAsync(int page, int pageSize)
        => await SendAuthorizedAsync<InHouseClosedGameSessionsPageDto>(
            HttpMethod.Get,
            $"api10/inhouse/sessions/closed?page={page}&pageSize={pageSize}")
            ?? new InHouseClosedGameSessionsPageDto { Page = page, PageSize = pageSize };

    public async Task<InHouseGameSessionDetailDto?> GetSessionAsync(Guid sessionId)
        => await SendAuthorizedAsync<InHouseGameSessionDetailDto>(
            HttpMethod.Get,
            $"api10/inhouse/sessions/{sessionId}");

    public async Task DeleteSessionAsync(Guid sessionId)
        => await SendAuthorizedNoContentAsync(
            HttpMethod.Delete,
            $"api10/inhouse/sessions/{sessionId}");

    private async Task<TResponse?> SendAuthorizedAsync<TResponse>(HttpMethod method, string url)
    {
        using var request = new HttpRequestMessage(method, url);
        await AddAuthorizationAsync(request);
        using var response = await http.SendAsync(request);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return default;
        }

        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<TResponse>();
    }

    private async Task SendAuthorizedNoContentAsync(HttpMethod method, string url)
    {
        using var request = new HttpRequestMessage(method, url);
        await AddAuthorizationAsync(request);
        using var response = await http.SendAsync(request);
        await EnsureSuccessAsync(response);
    }

    public async Task<ReplayDetails?> GetReplayDetails(string replayHash)
    {
        try
        {
            var _httpClient = httpClientFactory.CreateClient("api");
            var response = await _httpClient.GetAsync($"api10/Replays/{replayHash}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<ReplayDetails>();
        }
        catch (Exception)
        {
            return null;
        }
    }

    private async Task<TResponse> PostAuthorizedAsync<TRequest, TResponse>(string url, TRequest content)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(content),
        };
        await AddAuthorizationAsync(request);
        using var response = await http.SendAsync(request);
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<TResponse>()
            ?? throw new InvalidOperationException("The server returned an empty response.");
    }

    private async Task AddAuthorizationAsync(HttpRequestMessage request)
    {
        var accessToken = await auth.GetAccessTokenAsync();
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new InvalidOperationException("You are not signed in.");
        }

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }

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
                // Raw body is good enough when the API did not return the usual error shape.
            }
        }

        throw new InvalidOperationException(string.IsNullOrWhiteSpace(message)
            ? $"Request failed with status {(int)response.StatusCode}."
            : message);
    }

    private async Task<TResponse> SendAuthorizedAsync<TRequest, TResponse>(HttpMethod method, string url, TRequest content)
    {
        using var request = new HttpRequestMessage(method, url)
        {
            Content = JsonContent.Create(content),
        };
        await AddAuthorizationAsync(request);
        using var response = await http.SendAsync(request);
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<TResponse>()
            ?? throw new InvalidOperationException("The server returned an empty response.");
    }
}
