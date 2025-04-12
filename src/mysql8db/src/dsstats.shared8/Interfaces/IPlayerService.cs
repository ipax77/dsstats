using dsstats.shared;

namespace dsstats.shared8.Interfaces;

public interface IPlayerService
{
    Task<List<PlayerCmdrAvgGain>> GetPlayerIdPlayerCmdrAvgGain(PlayerId playerId, RatingNgType ratingType, TimePeriod timePeriod, CancellationToken token);
    Task<PlayerStatsResponse> GetPlayerStats(PlayerId playerId, RatingNgType ratingNgType, CancellationToken token);
}