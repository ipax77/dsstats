namespace pax.dsstats.shared;

public record ReplaysRequest
{
    public List<TableOrder> Orders { get; set; } = new List<TableOrder>() { new TableOrder() { Property = "GameTime" } };
    public DateTime StartTime { get; set; } = new DateTime(2022, 2, 1);
    public DateTime? EndTime { get; set; } = null;
    public int Skip { get; set; }
    public int Take { get; set; }
    public string? Tournament { get; set; }
    public string? SearchString { get; set; }
    public string? SearchPlayers { get; set; }
    public bool LinkSearch { get; set; }
    public string? ReplayHash { get; set; }
    public List<GameMode> GameModes { get; set; } = new();
}

public record Order
{
    public string Property { get; set; } = "";
    public bool Ascending { get; set; }
}