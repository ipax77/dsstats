
using pax.dsstats.shared.Raven;

namespace pax.dsstats.shared;

public interface IRatingRepository
{
    Task DeleteRatings();
    Task<UpdateResult> UpdateMmrChanges(List<MmrChange> replayPlayerMmrChanges);
    Task<UpdateResult> UpdateRavenPlayers(Dictionary<RavenPlayer, RavenRating> ravenPlayerRatings, RatingType ratingType);

    Task<RatingsResult> GetRatings(RatingsRequest request, CancellationToken token);
    Task<RavenPlayer?> GetPlayerRating(int toonId, CancellationToken token = default);
    Task<List<MmrDevDto>> GetRatingsDeviation();
    Task<List<MmrDevDto>> GetRatingsDeviationStd();
    Task<List<MmrChange>> GetReplayPlayerMmrChanges(List<int> replayPlayerIds, CancellationToken token = default);
    List<RequestNames> GetTopPlayersStd(int minGames);
    List<RequestNames> GetTopPlayersCmdr(int minGames);
    Task<string?> GetToonIdName(int toonId);
}
