using pax.dsstats.shared.Arcade;

namespace pax.dsstats.shared;

public record MmrOptions
{
    public const double consistencyImpact = 0.50;
    public const double consistencyDeltaMult = 0.15;

    public const double confidenceImpact = 1.0;//0.95;
    public const double distributionMult = 1.0 / (1/*2*/);

    public const double antiSynergyPercentage = 0.50;
    public const double synergyPercentage = 1 - antiSynergyPercentage;
    public const double ownMatchupPercentage = 1.0 / 3;
    public const double matesMatchupsPercentage = (1 - ownMatchupPercentage) / 2;

    public MmrOptions(bool reCalc, double eloK = 168, double clip = 1600)
    {
        ReCalc = reCalc;

        UseEquality = false;

        UseCommanderMmr = false;
        UseConsistency = true;
        UseFactorToTeamMates = false;
        UseConfidence = true;

        StartMmr = 1000;
        EloK = eloK;
        Clip = clip;
    }

    public double StartMmr { get; init; } // default 1000
    public double EloK { get; init; } // default 32
    public double Clip { get; init; } // default 400

    public bool UseEquality { get; init; }

    public bool UseCommanderMmr { get; init; }
    public bool UseConsistency { get; init; }
    public bool UseFactorToTeamMates { get; init; }
    public bool UseConfidence { get; init; }

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
