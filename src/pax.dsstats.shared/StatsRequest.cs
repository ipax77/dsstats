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
