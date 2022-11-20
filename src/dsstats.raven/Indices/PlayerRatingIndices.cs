
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