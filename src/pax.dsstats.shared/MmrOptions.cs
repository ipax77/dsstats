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

        DoubleAtDays = 440;

        StartMmr = 1000;
        BalanceDeviationOffset = StandardMatchDeviation * 0.05;
        StandardPlayerDeviation = StartMmr / 3/*zScore*/;
        StandardMatchDeviation = Math.Sqrt(6/*totalPlayerAmount*/ * Math.Pow(StandardPlayerDeviation, 2));
        EloK = StandardPlayerDeviation * 0.75;
    }

    public double StartMmr { get; init; }
    public double StandardPlayerDeviation { get; init; }
    public double StandardMatchDeviation { get; init; }
    public double BalanceDeviationOffset { get; init; }
    public double EloK { get; init; }
    public double DoubleAtDays { get; init; } // after x days without playing, the decayFactor = * 2.0

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
