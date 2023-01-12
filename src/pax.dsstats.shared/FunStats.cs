
namespace pax.dsstats.shared;

public record FunStats
{
    public TimeSpan TotalDuration { get; set; }
    public TimeSpan AvgDuration { get; set; }
    public List<PosInfo> PosInfos { get; set; } = new();
    public UnitInfo? MostBuildUnit { get; set; }
    public UnitInfo? LeastBuildUnit { get; set; }
    public ReplayDetailsDto? FirstReplay { get; set; }
    public ReplayDetailsDto? GreatestArmyReplay { get; set; }
    public ReplayDetailsDto? MostUpgradesReplay { get; set; }
    public ReplayDetailsDto? MostCompetitiveReplay { get; set; }
    public ReplayDetailsDto? GreatestComebackReplay { get; set; }
}

public record UnitInfo
{
    public string UnitName { get; set; } = string.Empty;
    public int Count { get; set; }
}

public record PosInfo
{
    public int Pos { get; set; }
    public int Count { get; set; }
    public int Wins { get; set; }
}

public record FunReplayInfo
{
    public DateTime GameTime { get; set; }
    public Commander Commander { get; set; }
    public int Duration { get; set; }
    public int Army { get; set; }
    public int Upgrades { get; set; }
    public string ReplayHash { get; set; } = string.Empty;
}