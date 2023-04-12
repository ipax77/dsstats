namespace pax.dsstats.shared.Arcade;

public record ArcadeReplaysRequest
{
    public string? Search { get; set; }
    public GameMode GameMode { get; set; }
    public int RegionId { get; set; }
    public bool TournamentEdition { get; set; }
    public int Skip { get; set; }
    public int Take { get; set; }
    public List<TableOrder> Orders { get; set; } = new List<TableOrder>() { new TableOrder() { Property = "CreatedAt" } };
    public int ReplayId { get; set; }
    public int PlayerId { get; set; }
    public int PlayerIdWith { get; set; }
    public int PlayerIdVs { get; set; }
    public string? ProfileName { get; set; }
}
