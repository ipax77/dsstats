using FireMath.NET;
using pax.dsstats.shared.Arcade;

namespace pax.dsstats.shared;

public record MmrOptions
{
    public const double antiSynergyPercentage = 0.50;
    public const double synergyPercentage = 1 - antiSynergyPercentage;
    public const double ownMatchupPercentage = 1.0 / 3;
    public const double matesMatchupsPercentage = (1 - ownMatchupPercentage) / 2;

    public MmrOptions(bool reCalc)
    {
        ReCalc = reCalc;

        UseCommanderMmr = false;

        StandardPlayerDeviation = 400;
        StandardMatchDeviation = ((3 + 3) * StandardPlayerDeviation.Pow()).Sqrt();
        StartMmr = 1000;
    }

    public double StandardPlayerDeviation { get; init; } // default 800
    public double StandardMatchDeviation { get; init; }
    public double StartMmr { get; init; } // default 1000

    public bool UseCommanderMmr { get; init; }

    public bool ReCalc { get; set; }
    public Dictionary<RatingType, Dictionary<ArcadePlayerId, Dictionary<int, double>>> InjectDic { get; set; } = new();

    public double GetInjectRating(RatingType ratingType, DateTime gameTime, ArcadePlayerId playerId)
    {
        if (InjectDic[ratingType].TryGetValue(playerId, out var plEnt))
        {
            int dateInt = int.Parse(gameTime.ToString(@"yyyyMMdd"));
            if (plEnt.TryGetValue(dateInt, out double rating))
            {
                return rating;
            }
            else
            {
                double dateRating = 1000.0;
                foreach (var ds in plEnt.Keys.OrderBy(o => o))
                {
                    if (ds > dateInt)
                    {
                        break;
                    }
                    dateRating = plEnt[ds];
                }
                return dateRating;
            }
        }
        return 1000.0;
    }
}
