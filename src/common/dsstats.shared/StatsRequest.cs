namespace dsstats.shared;

public sealed record WinrateEnt
{
    public Commander Commander { get; init; }
    public int Count { get; init; }
    public int Wins { get; init; }
    public double AvgRating { get; init; }
    public double AvgPerformance { get; init; }
    public int Replays { get; init; }
}

public sealed class StatsRequest
{
    public StatsType Type { get; set; }
    public TimePeriod TimePeriod { get; set; }
    public RatingType RatingType { get; set; }
    public Commander Interest { get; set; }
    public bool WithLeavers { get; set; }
}

public interface IStatsResponse { }

public sealed class WinrateResponse : IStatsResponse
{
    public List<WinrateEnt> WinrateEnts { get; set; } = [];
}

public sealed class SynergyResponse : IStatsResponse
{
    public List<object> SynergyEnts { get; set; } = [];
}

public sealed class StatsResponse : IStatsResponse
{
    public List<object> StatsEnts { get; set; } = [];
}


public sealed class DashboardStatsResponse
{
    public int Total { get; init; }
    public int SC2Arcade { get; init; }
    public int Dsstats { get; init; }
    public List<DashboardGameModeStats> GameModes { get; init; } = [];
    public int Uploads { get; init; }
}

public sealed class DashboardGameModeStats
{
    public GameMode GameMode { get; set; }
    public int Count { get; set; }
}