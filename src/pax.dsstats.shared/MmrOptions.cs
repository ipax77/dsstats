namespace pax.dsstats.shared;

public record MmrOptions
{
    public MmrOptions(bool reCalc)
    {
        this.ReCalc = reCalc;
    }

    public bool ReCalc { get; set; }
    public bool UseCommanderMmr { get; init; } = false;
    public bool UseConsistency { get; init; } = true;
    public bool UseFactorToTeamMates { get; init; } = false;
    public bool UseConfidence { get; init; } = true;
}