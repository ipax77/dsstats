using dsstats.shared;
using dsstats.shared.Interfaces;
using System.Net.Http.Json;

namespace dsstats.apiServices;

public class ReplayRepository(IHttpClientFactory httpClientFactory) : IReplayRepository
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("api");

    public async Task<ReplayDetails?> GetReplayDetails(string replayHash)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api10/Replays/{replayHash}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<ReplayDetails>();
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task<ReplayRatingDto?> GetReplayRating(string replayHash)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api10/Replays/rating/{replayHash}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<ReplayRatingDto>();
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task<List<ReplayListDto>> GetReplays(ReplaysRequest request, CancellationToken token = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api10/Replays/list", request, token);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<ReplayListDto>>(cancellationToken: token) ?? [];
        }
        catch (Exception)
        {
            return [];
        }
    }

    public async Task<int> GetReplaysCount(ReplaysRequest request, CancellationToken token = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api10/Replays/listcount", request, token);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<int>(cancellationToken: token);
        }
        catch (Exception)
        {
            return 0;
        }
    }

    public async Task<ReplayDetails?> GetArcadeReplayDetails(string replayHash)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api10/Replays/arcade/{replayHash}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<ReplayDetails>();
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task<List<ReplayListDto>> GetArcadeReplays(ArcadeReplaysRequest request, CancellationToken token = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api10/Replays/arcade/list", request, token);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<ReplayListDto>>(cancellationToken: token) ?? [];
        }
        catch (Exception)
        {
            return [];
        }
    }

    public async Task<int> GetArcadeReplaysCount(ArcadeReplaysRequest request, CancellationToken token = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api10/Replays/arcade/listcount", request, token);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<int>(cancellationToken: token);
        }
        catch (Exception)
        {
            return 0;
        }
    }

    public async Task<ReplayDetails?> GetLatestReplay()
    {
        return null;
    }

    public Task SaveReplayRatingAll(string replayHash, ReplayRatingDto rating)
    {
        return Task.CompletedTask;
    }

    public async Task<ReplayDetails?> GetNextReplay(bool after, string replayHash)
    {
        return null;
    }
}
