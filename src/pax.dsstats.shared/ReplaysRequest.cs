using pax.dsstats.shared.Arcade;

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
    public bool TEMaps { get; set; }
    public int PlayerCount { get; set; }
    public List<GameMode> GameModes { get; set; } = new();
    public bool WithMmrChange { get; set; }
    public int ToonId { get; set; }
    public int ToonIdWith { get; set; }
    public int ToonIdVs { get; set; }
    public string? ToonIdName { get; set; }
    public ReplaysAdvancedRequest? AdvancedRequest { get; set; }
}

public record ReplaysAdvancedRequest
{
    public ReplaysAdvancedRequest()
    {
        for (int i = 0; i < 3; i++)
        {
            ReplayCmdrRequests.Add(new() { Position = i + 1 });
            ReplayNameRequests.Add(new() { Position = i + 1 });
        }
    }

    public List<ReplayCmdrRequest> ReplayCmdrRequests { get; set; } = new();
    public List<ReplayNameRequest> ReplayNameRequests { get; set; } = new();

    public void Clear()
    {
        foreach (var ent in ReplayCmdrRequests)
        {
            ent.Clear();
        }
        foreach (var ent in ReplayNameRequests)
        {
            ent.Clear();
        }
    }
}

public record ReplayCmdrRequest
{
    public Commander Commander { get; set; }
    public Commander OppCommander { get; set; }
    public int Position { get; set; }
    public ReplaysAdvEnum Option { get; set; } = ReplaysAdvEnum.Exact;

    public void Clear()
    {
        Commander = Commander.None;
        OppCommander = Commander.None;
        Option = ReplaysAdvEnum.Exact;
    }
}

public record ReplayNameRequest
{
    public string Name { get; set; } = string.Empty;
    public string OppName { get; set; } = string.Empty;
    public int Position { get; set; }
    public ReplaysAdvEnum Option { get; set; } = ReplaysAdvEnum.Exact;

    public void Clear()
    {
        Name = string.Empty;
        OppName = string.Empty;
        Option = ReplaysAdvEnum.Exact;
    }
}

public record Order
{
    public string Property { get; set; } = "";
    public bool Ascending { get; set; }
}

public enum ReplaysAdvEnum
{
    Any = 0,
    Exact = 1,
    ExactLine = 2
}