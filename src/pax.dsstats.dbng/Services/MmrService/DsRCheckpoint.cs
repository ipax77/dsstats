namespace pax.dsstats.dbng.Services;

public record DsRCheckpoint
{
    public int Index { get; set; }
    public double Consistency { get; init; }
    public double Uncertainty { get; init; }
    public double Mmr { get; init; }
    public DateTime Time { get; init; }
}