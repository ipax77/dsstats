using dsstats.shared;
using dsstats.shared.Interfaces;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace dsstats.apiServices;

public class ReplaysService : IReplaysService
{
    private readonly HttpClient httpClient;
    private readonly ILogger<ReplaysService> logger;
    private readonly string replaysController = "api8/v1/replays";

    public ReplaysService(HttpClient httpClient, ILogger<ReplaysService> logger)
    {
        this.httpClient = httpClient;
        this.logger = logger;
    }

    public async Task<ArcadeReplayDto?> GetArcadeReplay(string hash, CancellationToken token = default)
    {
        try
        {
            return await httpClient
                .GetFromJsonAsync<ArcadeReplayDto>($"{replaysController}/arcadereplay/{hash}", token);
        }
        catch (Exception ex)
        {
            logger.LogError("failed getting arcadereplay {hash}: {error}", hash, ex.Message);
        }
        return null;
    }

    public async Task<ArcadeReplayDto?> GetDssstatsArcadeReplay(string replayHash, CancellationToken token = default)
    {
        try
        {
            return await httpClient
                .GetFromJsonAsync<ArcadeReplayDto>($"{replaysController}/dsstatsarcadereplay/{replayHash}", token);
        }
        catch (Exception ex)
        {
            logger.LogError("failed getting arcadereplay {hash}: {error}", replayHash, ex.Message);
        }
        return null;
    }

    public async Task<ReplayDto?> GetReplay(string replayHash, bool dry = false, CancellationToken token = default)
    {
        try
        {
            return await httpClient
                .GetFromJsonAsync<ReplayDto>($"{replaysController}/replay/{dry}/{replayHash}", token);
        }
        catch (Exception ex)
        {
            logger.LogError("failed getting replay {hash}: {error}", replayHash, ex.Message);
        }
        return null;
    }

    public async Task<ReplayRatingDto?> GetReplayRating(string replayHash, bool comboRating)
    {
        try
        {
            return await httpClient
                .GetFromJsonAsync<ReplayRatingDto?>($"{replaysController}/replayrating/{comboRating}/{replayHash}");
        }
        catch (Exception ex)
        {
            logger.LogError("failed getting replayrating {hash}: {error}", replayHash, ex.Message);
        }
        return null;
    }

    public async Task<ReplaysResponse> GetReplays(ReplaysRequest request, CancellationToken token = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync($"{replaysController}/replays", request, token);
            response.EnsureSuccessStatusCode();
            var data = await response.Content.ReadFromJsonAsync<ReplaysResponse>(token);

            if (data == null)
            {
                logger.LogError($"failed getting replays");
            }
            else
            {
                return data;
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            logger.LogError("failed getting replays: {error}", ex.Message);
        }
        return new();
    }

    public async Task<int> GetReplaysCount(ReplaysRequest request, CancellationToken token = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync($"{replaysController}/count", request, token);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<int>(token);

        }
        catch (Exception ex)
        {
            logger.LogError("failed getting replays count: {error}", ex.Message);
        }
        return 0;
    }
}
