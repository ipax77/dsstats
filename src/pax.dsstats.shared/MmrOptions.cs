using FireMath.NET;

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
}
