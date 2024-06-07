namespace dsstats.shared;

public record PickBanStateDto
{
    public PickBanMode PickBanMode { get; set; }
    public int Visitors { get; set; }
    public int TotalBans { get; set; }
    public int TotalPicks { get; set; }
    public List<PickBan> Picks { get; set; } = [];
    public List<PickBan> Bans { get; set; } = [];
}

public record PickBan
{
    public int Slot { get; set; }
    public Commander Commander { get; set; }
    public string? Name { get; set; }
    public bool Locked { get; set; }
}