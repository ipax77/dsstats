namespace pax.dsstats.shared;

public record ReplaysRequest
{
    public List<TableOrder> Orders { get; set; } = new List<TableOrder>() { new TableOrder() { Property = "GameTime" } };
    public int Skip { get; set; }
    public int Take { get; set; }
    public string? Tournament { get; set; }
    public string? SearchString { get; set; }
    public string? SearchPlayers { get; set; }
    public bool LinkSearch { get; set; }
    public bool ResultAdjusted { get; set; }
    public string? ReplayHash { get; set; }
    public bool DefaultFilter { get; set; }
    public int PlayerCount { get; set; }
    public List<GameMode> GameModes { get; set; } = new();
    public bool WithMmrChange { get; set; }
    public int ToonId { get; set; }
    public int ToonIdWith { get; set; }
    public int ToonIdVs { get; set; }
    public string? ToonIdName { get; set; }
}

public record Order
{
    public string Property { get; set; } = "";
    public bool Ascending { get; set; }
}