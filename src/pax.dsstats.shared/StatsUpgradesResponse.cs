namespace pax.dsstats.shared;

public record StatsUpgradesResponse
{
    public Dictionary<Commander, List<StatsUpgradesBpInfo>> BpInfos { get; set; } = new();
}

public record StatsUpgradesBpInfo
{
    public Breakpoint Breakpoint { get; set; }
    public int Count { get; set; }
    public int UpgradeSpent { get; set; }
    public int ArmyValue { get; set; }
    public int Kills { get; set; }
}