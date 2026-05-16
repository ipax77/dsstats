using System.Net;
using System.Net.Http.Json;
using dsstats.shared.InHouse;

namespace dsstats.apiServices;

public sealed class InHouseClosedGameSessionService(IHttpClientFactory httpClientFactory) : IInHouseClosedGameSessionService
{
    private readonly HttpClient httpClient = httpClientFactory.CreateClient("api");

    public async Task<InHouseClosedGameSessionsPageDto> GetClosedSessionsAsync(
        InHouseClosedGameSessionsRequest request,
        CancellationToken token = default)
    {
        try
        {
            var url = $"api10/inhouse/sessions/closed?page={request.NormalizedPage}&pageSize={request.NormalizedPageSize}";
            return await httpClient.GetFromJsonAsync<InHouseClosedGameSessionsPageDto>(url, token)
                ?? new InHouseClosedGameSessionsPageDto
                {
                    Page = request.NormalizedPage,
                    PageSize = request.NormalizedPageSize,
                };
        }
        catch (Exception)
        {
            return new()
            {
                Page = request.NormalizedPage,
                PageSize = request.NormalizedPageSize,
            };
        }
    }

    public async Task<InHouseClosedGameSessionDetailDto?> GetClosedSessionAsync(
        Guid sessionId,
        CancellationToken token = default)
    {
        try
        {
            var response = await httpClient.GetAsync($"api10/inhouse/sessions/closed/{sessionId}", token);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<InHouseClosedGameSessionDetailDto>(cancellationToken: token);
        }
        catch (Exception)
        {
            return null;
        }
    }
}
