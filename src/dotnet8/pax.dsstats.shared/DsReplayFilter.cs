namespace pax.dsstats.shared;
public record DsReplayFilter
{
    public List<TableOrder> Orders { get; set; } = new List<TableOrder>() { new TableOrder() { Property = "GameTime" } };
    public int Skip { get; set; } = 0;
    public int Take { get; set; } = 25;
    public DateTime StartTime { get; set; } = new DateTime(2022, 2, 1);
    public DateTime? EndTime { get; set; } = null;
    public bool ShowGroupGames { get; set; }
    public string? SearchString { get; set; }
    public HashSet<string>? SearchStrings => SearchString == null ? null : SearchString.Split(' ').Where(x => x.Length > 0).ToHashSet();
}

public record TableOrder
{
    public string Property { get; set; } = "";
    public bool Ascending { get; set; }
}