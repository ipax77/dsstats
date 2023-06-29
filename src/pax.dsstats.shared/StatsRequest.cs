using System.Text.Json.Serialization;

namespace pax.dsstats.shared;

public record StatsRequest
{
    public StatsMode StatsMode { get; set; }
    public TimePeriod TimePeriod { get; set; } = TimePeriod.Past90Days;
    [JsonIgnore]
    public bool BeginAtZero { get; set; }
    [JsonIgnore]
    public List<Commander> AddRemoveCommanders { get; set; } = new();
    public Commander Interest { get; set; }
    public Commander Versus { get; set; }
    public bool Uploaders { get; set; }
    public bool DefaultFilter { get; set; } = true;
    public bool TeMaps { get; set; }
    public int PlayerCount { get; set; }
    public List<RequestNames> PlayerNames { get; set; } = new();
    public List<GameMode> GameModes { get; set; } = new();
    public string? Tournament { get; set; }
    public string? Round { get; set; }
}

public enum StatsMode
{
    None = 0,
    Winrate = 1,
    Timeline = 2,
    Mvp = 3,
    Synergy = 4,
    Count = 5,
    Duration = 6,
}

public record WinrateEnt
{
    public Commander Commander { get; set; }
    public int Count { get; set; }
    public int Wins { get; set; }
    public double AvgRating { get; set; }
    public double AvgGain { get; set; }
}


public record WinrateRequest
{
    public TimePeriod TimePeriod { get; set; }
    public RatingType RatingType { get; set; }
    public Commander Interest { get; set; }
    public WinrateType WinrateType { get; set; }
    public int FromRating { get; set; }
    public int ToRating { get; set; }
}

public record WinrateResponse
{
    public Commander Interest { get; set; }
    public List<WinrateEnt> WinrateEnts { get; set; } = new();
}

public enum WinrateType
{
    AvgGain = 0,
    Winrate = 1,
    Matchups = 2,
    AvgRating = 3,
}