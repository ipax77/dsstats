﻿using System.Text.Json.Serialization;

namespace pax.dsstats.shared;

public record StatsRequest
{
    public StatsMode StatsMode { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    [JsonIgnore]
    public string TimePeriod { get; set; } = "This Year";
    [JsonIgnore]
    public bool BeginAtZero { get; set; }
    public Commander Interest { get; set; }
    public Commander Versus { get; set; }
    public bool Uploaders { get; set; }
    public bool DefaultFilter { get; set; } = true;
    public int PlayerCount { get; set; }
    public List<string> PlayerNames { get; set; } = new();
    public List<GameMode> GameModes { get; set; } = new();
    public string? Tournament { get; set; }
    public string? Round { get; set; }
}

public enum StatsMode
{
    None = 0,
    Winrate = 1,
    Timeline = 2
}