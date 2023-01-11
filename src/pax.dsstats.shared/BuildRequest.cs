namespace pax.dsstats.shared;

public record BuildRequest
{
    public List<RequestNames> PlayerNames { get; set; } = new();
    public Commander Interest { get; set; }
    public Commander Versus { get; set; }
    public TimePeriod Timespan { get; set; } = TimePeriod.Patch2_60;
}

public record RequestNames
{
    public string Name { get; set; } = "";
    public int ToonId { get; init; }
    public int RegionId { get; init; }
}