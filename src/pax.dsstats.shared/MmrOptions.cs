using FireMath.NET;

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

    public MmrOptions(bool reCalc)
    {
        ReCalc = reCalc;

        UseEquality = false;

        UseCommanderMmr = false;
        UseConsistency = true;
        UseFactorToTeamMates = false;
        UseConfidence = true;

        StandardPlayerDeviation = 400;
        StandardMatchDeviation = ((3 + 3) * StandardPlayerDeviation.Pow()).Sqrt();
        StartMmr = 1000;
    }

    public double StandardPlayerDeviation { get; init; } // default 800
    public double StandardMatchDeviation { get; init; }
    public double StartMmr { get; init; } // default 1000

    public bool UseEquality { get; init; }

    public bool UseCommanderMmr { get; init; }
    public bool UseConsistency { get; init; }
    public bool UseFactorToTeamMates { get; init; }
    public bool UseConfidence { get; init; }

    public bool ReCalc { get; set; }
}
