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
    public StatsFilter? Filter { get; set; }
}

public sealed class StatsFilter
{
    public FilterRange<DateTime> DateRange { get; set; } = new() { From = DateTime.Today.AddDays(-90), To = DateTime.Today };
    public FilterRange<int> RatingRange { get; set; } = new() { From = Data.MinBuildRating, To = Data.MaxBuildRating };
    public FilterRange<int> DurationRange { get; set; } = new() { From = Data.MinDuration, To = Data.MaxDuration };

    public void Reset()
    {
        DateRange = new() { From = DateTime.Today.AddDays(-90), To = DateTime.Today };
        RatingRange = new() { From = Data.MinBuildRating, To = Data.MaxBuildRating };
        DurationRange = new() { From = Data.MinDuration, To = Data.MaxDuration };
    }
}

public sealed class FilterRange<T>
{
    public T From { get; set; } = default!;
    public T To { get; set; } = default!;
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