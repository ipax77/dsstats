
namespace dsstats.shared;

public record IhReplay
{
    public ReplayDto Replay { get; set; } = null!;
    public ReplayMetadata Metadata { get; set; } = null!;
}

public record ReplayMetadata
{
    public List<ReplayMetadataPlayer> Players { get; set; } = [];
}

public record ReplayMetadataPlayer
{
    public PlayerId PlayerId { get; set; } = new();
    public string Name { get; set; } = string.Empty;
    public bool Observer { get; set; }
    public int Id { get; set; }
    public int SlotId { get; set; }
    public Commander SelectedRace { get; set; }
    public Commander AssignedRace { get; set; }

}