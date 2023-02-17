namespace pax.dsstats.shared;

public record DistributionRequest
{
    public RatingType RatingType { get; set; }
    public TimePeriod TimePeriod { get; set; } = TimePeriod.All;
    public int ToonId { get; set; }
    public Commander Interest { get; set; }
}

public record DistributionResponse
{
    public List<MmrDevDto> MmrDevs { get; set; } = new();
}
