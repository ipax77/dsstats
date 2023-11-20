
namespace dsstats.shared;

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