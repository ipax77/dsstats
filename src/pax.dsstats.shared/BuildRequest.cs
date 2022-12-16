namespace pax.dsstats.shared;

public record BuildRequest
{
    public List<RequestNames> PlayerNames { get; set; } = new();
    public Commander Interest { get; set; }
    public Commander Versus { get; set; }
    public string Timespan { get; set; } = "Patch 2.60";
    public DateTime StartTime { get; set; } = new DateTime(2020, 07, 28, 5, 23, 0);
    public DateTime EndTime { get; set; } = DateTime.Today;
}

public record RequestNames
{
    public string Name { get; set; } = "";
    public int ToonId { get; init; }
    public int RegionId { get; init; }
}