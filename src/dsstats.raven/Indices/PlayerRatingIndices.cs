
using pax.dsstats.shared;
using Raven.Client.Documents.Indexes;

namespace dsstats.raven;

public class PlayerRatingCmdr_ByPlayerId : AbstractIndexCreationTask<PlayerRatingCmdr>
{
    public PlayerRatingCmdr_ByPlayerId()
    {
        Map = toonIdPlayerRatings => from toonIdPlayerRating in toonIdPlayerRatings
                                     select new
                                     {
                                         PlayerId = toonIdPlayerRating.PlayerId
                                     };
    }
}

public class PlayerRatingCmdr_ByToonId : AbstractIndexCreationTask<PlayerRatingCmdr>
{
    public PlayerRatingCmdr_ByToonId()
    {
        Map = toonIdPlayerRatings => from toonIdPlayerRating in toonIdPlayerRatings
                                     select new
                                     {
                                         ToonId = toonIdPlayerRating.ToonId
                                     };
    }
}

public class PlayerRatingCmdr_ByGamesAndMainAndMainPercentageAndMmrAndMvprateAndRegionIdAndWinrateAndSearchName : AbstractIndexCreationTask<PlayerRatingCmdr>
{
    public PlayerRatingCmdr_ByGamesAndMainAndMainPercentageAndMmrAndMvprateAndRegionIdAndWinrateAndSearchName()
    {
        Map = ratings => from rating in ratings
                         where rating.IsUploader
                         select new
                         {
                             rating.Games,
                             rating.Main,
                             rating.MainPercentage,
                             rating.Mmr,
                             rating.Mvprate,
                             rating.RegionId,
                             rating.Winrate,
                             rating.Name
                         };
    }
}

public class PlayerRatingCmdr_Average_ByMmr : AbstractIndexCreationTask<PlayerRatingCmdr, PlayerRatingCmdr_Average_ByMmr.Result>
{
    public class Result
    {
        public int Mmr { get; set; }
        public int Count { get; set; }
    }

    public PlayerRatingCmdr_Average_ByMmr()
    {
        Map = ratings => from rating in ratings
                         select new
                         {
                             Mmr = (int)rating.Mmr,
                             Count = 1
                         };

        Reduce = results => from result in results
                            group result by result.Mmr into g
                            select new
                            {
                                Mmr = g.Key,
                                Count = g.Sum(s => s.Count)
                            };
    }
}

public class PlayerRatingStd_ByPlayerId : AbstractIndexCreationTask<PlayerRatingStd>
{
    public PlayerRatingStd_ByPlayerId()
    {
        Map = toonIdPlayerRatings => from toonIdPlayerRating in toonIdPlayerRatings
                                     select new
                                     {
                                         PlayerId = toonIdPlayerRating.PlayerId
                                     };
    }
}

public class PlayerRatingStd_ByToonId : AbstractIndexCreationTask<PlayerRatingStd>
{
    public PlayerRatingStd_ByToonId()
    {
        Map = toonIdPlayerRatings => from toonIdPlayerRating in toonIdPlayerRatings
                                     select new
                                     {
                                         ToonId = toonIdPlayerRating.ToonId
                                     };
    }
}

public class PlayerRatingStd_Average_ByMmr : AbstractIndexCreationTask<PlayerRatingStd, PlayerRatingStd_Average_ByMmr.Result>
{
    public class Result
    {
        public int Mmr { get; set; }
        public int Count { get; set; }
    }

    public PlayerRatingStd_Average_ByMmr()
    {
        Map = ratings => from rating in ratings
                         select new
                         {
                             Mmr = (int)rating.Mmr,
                             Count = 1
                         };

        Reduce = results => from result in results
                            group result by result.Mmr into g
                            select new
                            {
                                Mmr = g.Key,
                                Count = g.Sum(s => s.Count)
                            };
    }
}