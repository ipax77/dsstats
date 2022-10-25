namespace pax.dsstats.shared;

public record MmrDevDto
{
    public double Mmr { get; init; }
    public int Count { get; set; }
}
