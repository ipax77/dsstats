namespace pax.dsstats.shared;

public record TimeStat
{
    public string Label { get; init; } = "";
    public Commander Commander { get; init; }
    public double Winrate { get; init; }
    public int Count { get; init; }
}
