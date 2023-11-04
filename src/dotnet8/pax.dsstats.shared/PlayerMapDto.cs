namespace pax.dsstats.shared;

public class PlayerMapDto
{
    public int PlayerId { get; init; }
    public string Name { get; init; } = null!;
    public int ToonId { get; init; }
    public int RegionId { get; init; }
}
