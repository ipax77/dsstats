namespace pax.dsstats.shared.Arcade;

public interface IArcadeService
{
    Task<List<ArcadePlayerRatingDto>> GetRatings(ArcadeRatingsRequest request, CancellationToken token);
    Task<int> GetRatingsCount(ArcadeRatingsRequest request, CancellationToken token);
    Task<DistributionResponse> GetDistribution(DistributionRequest request, CancellationToken token = default);
    Task<List<ArcadeReplayListDto>> GetArcadeReplays(ArcadeReplaysRequest request, CancellationToken token);
    Task<int> GetReplayCount(ArcadeReplaysRequest request, CancellationToken token);
    Task<ArcadeReplayDto?> GetArcadeReplay(int id, CancellationToken token = default);
    Task<ArcadePlayerDetails> GetPlayerDetails(ArcadePlayerId playerId, CancellationToken token);
    Task<ArcadePlayerDetails> GetPlayerDetails(int arcadePlayerId, CancellationToken token);
    Task<ArcadePlayerMoreDetails> GetMorePlayerDatails(ArcadePlayerId playerId, RatingType ratingType, CancellationToken token);
    Task<List<ReplayPlayerChartDto>> GetPlayerRatingChartData(PlayerId playerId, RatingType ratingType);
    Task<RequestNames?> GetRequetNamesFromId(int playerId, CancellationToken token = default);
}
