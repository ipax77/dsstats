namespace dsstats.shared;

public sealed record CountEnt
{
    public Commander Commander { get; set; }
    public int Count { get; set; }
}

public sealed record WinrateEnt
{
    public Commander Commander { get; init; }
    public int Count { get; init; }
    public int Wins { get; init; }
    public double AvgRating { get; init; }
    public double AvgPerformance { get; init; }
    public int Replays { get; init; }
}

public sealed record SynergyEnt
{
    public Commander Commander { get; init; }
    public Commander Teammate { get; init; }
    public int Games { get; init; }
    public int Wins { get; init; }
    public double Winrate { get; init; }
    public double AvgGain { get; init; }
}

public sealed record TimelineEnt
{
    public Commander Commander { get; init; }
    public List<TimelineStep> Steps { get; init; } = [];
}

public sealed record TimelineStep
{
    public int BucketStart { get; init; }
    public int Count { get; init; }
    public int Wins { get; init; }
    public double AvgGain { get; init; }
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

public sealed class UserStatsRequest
{
    public StatsRequest Request { get; set; } = default!;
    public ToonIdDto ToonId { get; set; } = default!;
}

public sealed class StatsFilter
{
    public FilterRange<DateTime> DateRange { get; set; } = new() { From = DateTime.Today.AddDays(-90), To = DateTime.Today };
    public FilterRange<int> RatingRange { get; set; } = new() { From = Data.MinBuildRating, To = Data.MaxBuildRating };
    public FilterRange<int> DurationRange { get; set; } = new() { From = Data.MinDuration, To = Data.MaxDuration };
    public FilterRange<int> Exp2WinRange { get; set; } = new() { From = 0, To = 100 };
    public FilterRange<int> TeamRatingRange { get; set; } = new() { From = Data.MinBuildRating, To = Data.MaxBuildRating };

    public void Reset()
    {
        DateRange = new() { From = DateTime.Today.AddDays(-90), To = DateTime.Today };
        RatingRange = new() { From = Data.MinBuildRating, To = Data.MaxBuildRating };
        DurationRange = new() { From = Data.MinDuration, To = Data.MaxDuration };
        Exp2WinRange = new() { From = 0, To = 100 };
        TeamRatingRange = new() { From = Data.MinBuildRating, To = Data.MaxBuildRating };
    }

    public bool IsDefault()
    {
        return DateRange.From == DateTime.Today.AddDays(-90)
            && DateRange.To == DateTime.Today
            && RatingRange.From == Data.MinBuildRating
            && RatingRange.To == Data.MaxBuildRating
            && DurationRange.From == Data.MinDuration
            && DurationRange.To == Data.MaxDuration
            && Exp2WinRange.From == 0 && Exp2WinRange.To == 100
            && TeamRatingRange.From == Data.MinBuildRating && TeamRatingRange.To == Data.MaxBuildRating;
    }
}

public sealed class FilterRange<T>
{
    public T From { get; set; } = default!;
    public T To { get; set; } = default!;
}

public interface IStatsResponse { }

public sealed class CountResponse : IStatsResponse
{
    public int Count { get; set; }
    public int ReplaysWithLeaver { get; set; }
    public int ReplaysWithoutRating { get; set; }
    public int NoResult { get; set; }
    public int Under5Min { get; set; }
    public List<CountEnt> CountEnts { get; set; } = [];
}

public sealed class WinrateResponse : IStatsResponse
{
    public List<WinrateEnt> WinrateEnts { get; set; } = [];
}

public sealed class SynergyResponse : IStatsResponse
{
    public List<SynergyEnt> SynergyEnts { get; set; } = [];
}

public sealed class TimelineResponse : IStatsResponse
{
    public List<TimelineEnt> TimelineEnts { get; set; } = [];
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
    public List<DashboardUploadSourceStats> UploadStats { get; init; } = [];
}

public sealed class DashboardGameModeStats
{
    public GameMode GameMode { get; set; }
    public int Count { get; set; }
}

public sealed class DashboardUploadSourceStats
{
    public string Source { get; init; } = string.Empty;
    public int Count { get; init; }
    public List<DashboardUploadVersionStats> Versions { get; init; } = [];
}

public sealed class DashboardUploadVersionStats
{
    public string Version { get; init; } = string.Empty;
    public int Count { get; init; }
}
