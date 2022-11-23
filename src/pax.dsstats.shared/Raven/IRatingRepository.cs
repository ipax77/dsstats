
using pax.dsstats.shared.Raven;

namespace pax.dsstats.shared;

public interface IRatingRepository
{
    Task<UpdateResult> UpdateMmrChanges(List<MmrChange> replayPlayerMmrChanges);
    Task<UpdateResult> UpdateRavenPlayers(Dictionary<RavenPlayer, RavenRating> ravenPlayerRatings, RatingType ratingType);

    Task<RatingsResult> GetRatings(RatingsRequest request, CancellationToken token);
    Task<RavenPlayerDetailsDto> GetPlayerDetails(int toonId, CancellationToken token = default);
    Task<List<MmrDevDto>> GetRatingsDeviation();
    Task<List<MmrDevDto>> GetRatingsDeviationStd();
    Task<List<PlChange>> GetReplayPlayerMmrChanges(string replayHash, CancellationToken token = default);
    List<RequestNames> GetTopPlayers(RatingType ratingType, int minGames);
    Task<string?> GetToonIdName(int toonId);
    List<int> GetNameToonIds(string name);
    Task<Dictionary<int, CalcRating>> GetCalcRatings(RatingType ratingType, List<ReplayDsRDto> replays, List<int> toonIds);
}
