using pax.dsstats.shared;
using pax.dsstats.shared.Arcade;

namespace sc2dsstats.maui.Services;

public class ArcadeService : IArcadeService
{
    public Task<ArcadeReplayDto?> GetArcadeReplay(int id, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    public Task<List<ArcadeReplayListDto>> GetArcadeReplays(ArcadeReplaysRequest request, CancellationToken token)
    {
        throw new NotImplementedException();
    }

    public Task<DistributionResponse> GetDistribution(DistributionRequest request, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    public Task<ArcadePlayerMoreDetails> GetMorePlayerDatails(ArcadePlayerId playerId, RatingType ratingType, CancellationToken token)
    {
        throw new NotImplementedException();
    }

    public Task<ArcadePlayerDetails> GetPlayerDetails(ArcadePlayerId playerId, CancellationToken token)
    {
        throw new NotImplementedException();
    }

    public Task<ArcadePlayerDetails> GetPlayerDetails(int arcadePlayerId, CancellationToken token)
    {
        throw new NotImplementedException();
    }

    public Task<List<ReplayPlayerChartDto>> GetPlayerRatingChartData(PlayerId playerId, RatingType ratingType)
    {
        throw new NotImplementedException();
    }

    public Task<List<ArcadePlayerRatingDto>> GetRatings(ArcadeRatingsRequest request, CancellationToken token)
    {
        throw new NotImplementedException();
    }

    public Task<int> GetRatingsCount(ArcadeRatingsRequest request, CancellationToken token)
    {
        throw new NotImplementedException();
    }

    public Task<int> GetReplayCount(ArcadeReplaysRequest request, CancellationToken token)
    {
        throw new NotImplementedException();
    }

    public Task<RequestNames?> GetRequetNamesFromId(int playerId, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }
}
