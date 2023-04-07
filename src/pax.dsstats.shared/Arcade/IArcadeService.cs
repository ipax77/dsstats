namespace pax.dsstats.shared.Arcade;

public interface IArcadeService
{
    Task<List<ArcadePlayerRatingDto>> GetRatings(ArcadeRatingsRequest request, CancellationToken token);
    Task<int> GetRatingsCount(ArcadeRatingsRequest request, CancellationToken token);
    Task<DistributionResponse> GetDistribution(DistributionRequest request, CancellationToken token = default);
}
