namespace pax.dsstats.shared.Arcade;

public record ArcadePlayerId
{
    public ArcadePlayerId()
    {

    }

    public ArcadePlayerId(int profileId, int realmId, int regionId)
    {
        ProfileId = profileId;
        RealmId = realmId;
        RegionId = regionId;
    }

    public int ProfileId { get; set; }
    public int RealmId { get; set; }
    public int RegionId { get; set; }
}
