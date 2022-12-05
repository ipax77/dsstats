
namespace pax.dsstats.shared;

public record ImportReport
{
    public int BlobFiles { get; set; }
    public int ReplaysFromBlobs { get; set; }
    public int BlobPreparationDuration { get; set; }
    public int MappingDuration { get; set; }
    public int LocalDupsDuration { get; set; }
    public int LocalDupsHandled { get; set; }
    public int DbDupsDuration { get; set; }
    public int DbDupsHandled { get; set; }
    public int DbDupsDeleted { get; set; }
    public int SaveDuration { get; set; }
    public int SavedReplays { get; set; }
    public int NewPlayers { get; set; }
    public int NewUnits { get; set; }
    public int NewUpgrades { get; set; }
    public bool Success { get; set; }
    public string? Error { get; set; }
    public DateTime LatestReplay { get; set; }
    public List<ReplayDsRDto> ContinueReplays { get; set; } = new();
}