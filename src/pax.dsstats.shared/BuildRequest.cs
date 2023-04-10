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
    public RequestNames(string name, int toonId, int regionId, int realmId)
    {
        Name = name;
        ToonId = toonId;
        RegionId = regionId;
        RealmId = realmId;
    }

    public RequestNames() { }

    public string Name { get; set; } = "";
    public int ToonId { get; init; }
    public int RegionId { get; set; }
    public int RealmId { get; set; } = 1;
}