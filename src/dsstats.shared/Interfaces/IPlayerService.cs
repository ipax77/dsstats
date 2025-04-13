namespace dsstats.shared.Interfaces;

public interface IPlayerService
{
    Task<string?> GetPlayerIdName(PlayerId playerId);
    Task<int> GetRatingsCount(RatingsRequest request, CancellationToken token);
    Task<List<ComboPlayerRatingDto>> GetRatings(RatingsRequest request, CancellationToken token);
    Task<PlayerDetailSummary> GetPlayerPlayerIdSummary(PlayerId playerId, RatingNgType ratingType, CancellationToken token = default);
    Task<PlayerRatingDetails> GetPlayerIdPlayerRatingDetails(PlayerId playerId, RatingNgType ratingType, CancellationToken token = default);
    Task<List<PlayerCmdrAvgGain>> GetPlayerIdPlayerCmdrAvgGain(PlayerId playerId, RatingNgType ratingType, TimePeriod timePeriod, CancellationToken token);
    Task<PlayerDetailResponse> GetPlayerIdPlayerDetails(PlayerDetailRequest request, CancellationToken token = default);
    Task<List<ReplayPlayerChartDto>> GetPlayerRatingChartData(PlayerId playerId, RatingNgType ratingType, CancellationToken token);
    Task<List<CommanderInfo>> GetPlayerIdCommandersPlayed(PlayerId playerId, RatingNgType ratingType, CancellationToken token);
    Task<DistributionResponse> GetDistribution(DistributionRequest request);
}