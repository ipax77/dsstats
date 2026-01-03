namespace dsstats.shared.Interfaces;

public interface IPlayerService
{
    Task<List<PlayerRatingListItem>> GetRatings(PlayerRatingsRequest request, CancellationToken token = default);
    Task<int> GetRatingsCount(PlayerRatingsRequest request, CancellationToken token = default);
    Task<PlayerStatsResponse> GetPlayerStats(PlayerStatsRequest request, CancellationToken token = default);
    Task<RatingDetails> GetRatingDetails(PlayerStatsRequest request, CancellationToken token = default);
    Task<CmdrAvgGainResponse> GetCommandersPerformance(PlayerStatsRequest request, CancellationToken token);
    Task<CmdrStrenghtResponse> GetCmdrPlayerInfos(CmdrStrenghtRequest request, CancellationToken token = default);
    Task<DistributionResponse> GetRatingDistribution(DistributionRequest request);
}
