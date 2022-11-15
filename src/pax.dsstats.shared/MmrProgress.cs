namespace pax.dsstats.shared;

public record MmrProgress
{
    public double CmdrMmrStart { get; set; }
    public double StdMmrStart { get; set; }
    public List<double> CmdrMmrDeltas { get; set; } = new();
    public List<double> StdMmrDeltas { get; set; } = new();
}