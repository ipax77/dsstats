
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

public class PlayerRatingCmdr_ForTable : AbstractIndexCreationTask<PlayerRatingCmdr>
{
    public class Result
    {
        public int Games { get; set; }
        public Commander Main { get; set; }
        public double MainPercentage { get; set; }
        public double Mmr { get; set; }
        public int Wins { get; set; }
        public int Mvp { get; set; }
        public int RegionId { get; set; }
        public int ToonId { get; set; }
        public string Name { get; set; } = "";
        public double Winrate { get; set; }
        public double Mvprate { get; set; }
    }

    public PlayerRatingCmdr_ForTable()
    {
        Map = ratings => from rating in ratings
                         where rating.IsUploader
                         select new Result
                         {
                             Games = rating.Games,
                             Main = rating.Main,
                             MainPercentage = rating.MainPercentage,
                             Mmr = rating.Mmr,
                             Wins = rating.Wins,
                             RegionId = rating.RegionId,
                             ToonId = rating.ToonId,
                             Mvp = rating.Mvp,
                             Name = rating.Name,
                             Winrate = rating.Winrate,
                             Mvprate = rating.Mvprate
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

public class PlayerRatingStd_ForTable : AbstractIndexCreationTask<PlayerRatingStd>
{
    public class Result
    {
        public int Games { get; set; }
        public Commander Main { get; set; }
        public double MainPercentage { get; set; }
        public double Mmr { get; set; }
        public int Wins { get; set; }
        public int Mvp { get; set; }
        public int RegionId { get; set; }
        public int ToonId { get; set; }
        public string Name { get; set; } = "";
        public double Winrate { get; set; }
        public double Mvprate { get; set; }
    }

    public PlayerRatingStd_ForTable()
    {
        Map = ratings => from rating in ratings
                         where rating.IsUploader
                         select new Result
                         {
                             Games = rating.Games,
                             Main = rating.Main,
                             MainPercentage = rating.MainPercentage,
                             Mmr = rating.Mmr,
                             Wins = rating.Wins,
                             RegionId = rating.RegionId,
                             ToonId = rating.ToonId,
                             Mvp = rating.Mvp,
                             Name = rating.Name,
                             Winrate = rating.Winrate,
                             Mvprate = rating.Mvprate
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