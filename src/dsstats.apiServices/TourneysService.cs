﻿using dsstats.shared;
using dsstats.shared.Interfaces;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace dsstats.apiServices;

public class TourneysService(HttpClient httpClient, ILogger<TourneysService> logger) : ITourneysService
{
    private readonly string tourneysController = "api8/v1/tourneys";

    public Task<(string, string)?> DownloadReplay(string replayHash)
    {
        throw new NotImplementedException();
    }

    public async Task<List<GroupStateDto>> GetGroupStates()
    {
        try
        {
            return await httpClient.GetFromJsonAsync<List<GroupStateDto>>($"{tourneysController}/groups") ?? [];

        }
        catch (Exception ex)
        {
            logger.LogError("failed getting groupStates: {error}", ex.Message);
        }
        return [];
    }

    public async Task<IhSessionDto?> GetIhSession(Guid groupId)
    {
        try
        {
            return await httpClient.GetFromJsonAsync<IhSessionDto>($"{tourneysController}/ihsession/{groupId}");
        }
        catch (Exception ex)
        {
            logger.LogError("failed getting ihsession {groupId}: {error}", groupId, ex.Message);
        }
        return null;
    }

    public async Task<List<IhSessionListDto>> GetIhSessions(int skip, int take, CancellationToken token)
    {
        try
        {
            return await httpClient.GetFromJsonAsync<List<IhSessionListDto>>($"{tourneysController}/ihsessions/{skip}/{take}", token) ?? [];
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            logger.LogError("failed getting ihsessions: {error}", ex.Message);
        }
        return [];
    }

    public async Task<int> GetIhSessionsCount(CancellationToken token)
    {
        try
        {
            return await httpClient.GetFromJsonAsync<int>($"{tourneysController}/ihsessionscount");
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            logger.LogError("failed getting ihsessions count: {error}", ex.Message);
        }
        return 0;
    }

    public async Task<List<TourneysReplayListDto>> GetReplays(TourneysReplaysRequest request, CancellationToken token)
    {
        try
        {
            var result = await httpClient.PostAsJsonAsync($"{tourneysController}/replays", request, token);
            result.EnsureSuccessStatusCode();

            return await result.Content.ReadFromJsonAsync<List<TourneysReplayListDto>>() ?? new();
        }
        catch (Exception ex)
        {
            logger.LogError("failed getting replays: {error}", ex.Message);
        }
        return new();
    }

    public async Task<List<ReplayListDto>> GetReplays(Guid groupId)
    {
        try
        {
            return await httpClient.GetFromJsonAsync<List<ReplayListDto>>($"{tourneysController}/ihsessionreplays/{groupId}") ?? [];
        }
        catch (Exception ex)
        {
            logger.LogError("failed getting ihsession replays: {error}", ex.Message);
        }
        return [];
    }

    public async Task<int> GetReplaysCount(TourneysReplaysRequest request, CancellationToken token = default)
    {
        try
        {
            var result = await httpClient.PostAsJsonAsync($"{tourneysController}/replayscount", request, token);
            result.EnsureSuccessStatusCode();

            return await result.Content.ReadFromJsonAsync<int>();
        }
        catch (Exception ex)
        {
            logger.LogError("failed getting replays count: {error}", ex.Message);
        }
        return 0;
    }

    public async Task<List<TourneyDto>> GetTourneys()
    {
        try
        {
            return await httpClient.GetFromJsonAsync<List<TourneyDto>>($"{tourneysController}") ?? new();

        }
        catch (Exception ex)
        {
            logger.LogError("failed getting tourneys: {error}", ex.Message);
        }
        return new();
    }

    public async Task<TourneysStatsResponse> GetTourneyStats(TourneysStatsRequest statsRequest, CancellationToken token)
    {
        try
        {
            var result = await httpClient.PostAsJsonAsync($"{tourneysController}/stats", statsRequest, token);
            result.EnsureSuccessStatusCode();

            return await result.Content.ReadFromJsonAsync<TourneysStatsResponse>() ?? new();
        }
        catch (Exception ex)
        {
            logger.LogError("failed getting stats: {error}", ex.Message);
        }
        return new();
    }

    public Task SeedTourneys()
    {
        throw new NotImplementedException();
    }
}
