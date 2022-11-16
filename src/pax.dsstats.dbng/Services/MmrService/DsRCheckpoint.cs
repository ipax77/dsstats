namespace pax.dsstats.dbng.Services;

public record DsRCheckpoint
{
    public double Consistency { get; init; }
    public double Uncertainty { get; init; }
    public double Mmr { get; init; }
    public DateTime Time { get; init; }
}