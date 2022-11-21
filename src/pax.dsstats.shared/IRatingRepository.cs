
namespace pax.dsstats.shared;

public interface IRatingRepository
{
    Task DeleteRatings();
    Task<UpdateResult> UpdateReplayPlayerMmrChanges(List<ReplayPlayerMmrChange> replayPlayerMmrChanges);
    Task<PlayerRatingsResult> GetRatings(RatingsRequest request, CancellationToken token);
    Task<string?> GetPlayerRatings(int toonId, CancellationToken token = default);
    Task<PlayerRatingBase?> GetPlayerRating(int toonId, CancellationToken token = default);
    Task<List<MmrDevDto>> GetRatingsDeviation();
    Task<List<MmrDevDto>> GetRatingsDeviationStd();
    Task<List<ReplayPlayerMmrChange>> GetReplayPlayerMmrChanges(List<int> replayPlayerIds, CancellationToken token = default);
    Task<UpdateResult> UpdatePlayerRatings<T>(List<PlayerRatingBase> playerRatings) where T : PlayerRatingBase, new();
    List<RequestNames> GetTopPlayersStd(int minGames);
    List<RequestNames> GetTopPlayersCmdr(int minGames);
    Task<string?> GetToonIdName(int toonId);
}
