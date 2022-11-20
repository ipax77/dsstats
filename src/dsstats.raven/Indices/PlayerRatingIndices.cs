
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

public class PlayerInfo_ByPlayerId : AbstractIndexCreationTask<PlayerInfo>
{
    public PlayerInfo_ByPlayerId()
    {
        Map = infos => from info in infos
                            select new
                            {
                                PlayerId = info.PlayerId
                            };
    }
}

public class PlayerInfo_ByPlayerIdAndRatingTypeCmdr : AbstractIndexCreationTask<PlayerInfo>
{
    public class Result
    {
        public Rating? Rating { get; set; }
    }

    public PlayerInfo_ByPlayerIdAndRatingTypeCmdr()
    {
        Map = playerInfos => from playerInfo in playerInfos
                             select new Result
                             {
                                Rating = playerInfo.Ratings.Where(x => x.Type == RatingType.Cmdr).FirstOrDefault()
                             };
    }
}

public class PlayerInfo_ByPlayerIdAndRatingTypeStd : AbstractIndexCreationTask<PlayerInfo>
{
    public class Result
    {
        public Rating? Rating { get; set; }
    }

    public PlayerInfo_ByPlayerIdAndRatingTypeStd()
    {
        Map = playerInfos => from playerInfo in playerInfos
                             select new Result
                             {
                                Rating = playerInfo.Ratings.Where(x => x.Type == RatingType.Std).FirstOrDefault()
                             };
    }
}