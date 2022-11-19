
namespace pax.dsstats.shared;

public interface IRatingRepository
{
    Task DeleteRatings();
    Task<UpdateResult> UpdateReplayPlayerMmrChanges(List<ReplayPlayerMmrChange> replayPlayerMmrChanges);
    Task<UpdateResult> UpdatePlayerRatings(List<PlayerRating> playerRatings);
    Task<List<PlayerRating>> GetPlayerRatings(RatingsRequest request);
}
