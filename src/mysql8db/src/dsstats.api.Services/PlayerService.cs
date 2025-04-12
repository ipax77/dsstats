using dsstats.shared;
using dsstats.shared8;
using dsstats.shared8.Interfaces;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace dsstats.api.Services;

public class PlayerService(HttpClient httpClient, ILogger<PlayerService> logger) : IPlayerService
{
    private readonly string playerController = "api/v1/playerstats";

    public async Task<List<PlayerCmdrAvgGain>> GetPlayerIdPlayerCmdrAvgGain(PlayerId playerId,
                                                                            RatingNgType ratingType,
                                                                            TimePeriod timePeriod,
                                                                            CancellationToken token)
    {
        try
        {
            var request = $"{playerController}/avggain/{playerId.ToonId}/{playerId.RegionId}/{playerId.RealmId}/{(int)ratingType}/{(int)timePeriod}";
            var response = await httpClient.GetFromJsonAsync<List<PlayerCmdrAvgGain>>(request, token);
            ArgumentNullException.ThrowIfNull(response);
            return response;
        }
        catch (Exception ex)
        {
            logger.LogError("Failed getting player avggain: {error}", ex.Message);
        }
        return [];
    }

    public async Task<PlayerStatsResponse> GetPlayerStats(PlayerId playerId, RatingNgType ratingNgType, CancellationToken token)
    {
        try
        {
            var request = $"{playerController}/stats/{playerId.ToonId}/{playerId.RegionId}/{playerId.RealmId}/{(int)ratingNgType}";
            var response = await httpClient.GetFromJsonAsync<PlayerStatsResponse>(request, token);
            ArgumentNullException.ThrowIfNull(response);
            return response;
        }
        catch (Exception ex)
        {
            logger.LogError("Failed getting player stats: {error}", ex.Message);
        }
        return new();
    }
}


