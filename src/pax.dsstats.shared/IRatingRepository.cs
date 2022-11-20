
namespace pax.dsstats.shared;

public interface IRatingRepository
{
    Task DeleteRatings();
    Task<UpdateResult> UpdateReplayPlayerMmrChanges(List<ReplayPlayerMmrChange> replayPlayerMmrChanges);
    Task<UpdateResult> UpdatePlayerRatings(List<PlayerRating> playerRatings);
    Task<PlayerRatingsResult> GetRatings(RatingsRequest request, CancellationToken token);
    Task<string?> GetPlayerRatings(int toonId, CancellationToken token = default);
    Task<PlayerRating?> GetPlayerRating(int toonId, CancellationToken token = default);
    Task<List<MmrDevDto>> GetRatingsDeviation();
    Task<List<ReplayPlayerMmrChange>> GetReplayPlayerMmrChanges(List<int> replayPlayerIds, CancellationToken token = default);
    Task<UpdateResult> UpdatePlayerInfos(List<PlayerInfo> playerInfos, RatingType ratingType);
}
