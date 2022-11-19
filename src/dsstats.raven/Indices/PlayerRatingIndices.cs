
using pax.dsstats.shared;
using Raven.Client.Documents.Indexes;

namespace dsstats.raven;

public class PlayerRating_ByToonId : AbstractIndexCreationTask<PlayerRating>
{
    public PlayerRating_ByToonId()
    {
        Map = toonIdPlayerRatings => from toonIdPlayerRating in toonIdPlayerRatings
                            select new
                            {
                                ToonId = toonIdPlayerRating.ToonId
                            };
    }
}

public class PlayerRating_ByPlayerId : AbstractIndexCreationTask<PlayerRating>
{
    public PlayerRating_ByPlayerId()
    {
        Map = toonIdPlayerRatings => from toonIdPlayerRating in toonIdPlayerRatings
                            select new
                            {
                                PlayerId = toonIdPlayerRating.PlayerId
                            };
    }
}

public class ReplayPlayerMmrChange_ByReplayPlayerId : AbstractIndexCreationTask<ReplayPlayerMmrChange>
{
    public ReplayPlayerMmrChange_ByReplayPlayerId()
    {
        Map = replayPlayerMmrChanges => from replayPlayerMmrChange in replayPlayerMmrChanges
                            select new
                            {
                                ReplayPlayerId = replayPlayerMmrChange.ReplayPlayerId
                            };
    }
}