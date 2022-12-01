﻿using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace pax.dsstats.shared;

public record ReplayListDto
{
    public int ReplayId { get; set; }
    public DateTime GameTime { get; set; }
    public int Duration { get; set; }
    public int WinnerTeam { get; set; }
    public PlayerResult PlayerResult { get; set; }
    public int PlayerPos { get; set; }
    public GameMode GameMode { get; set; }
    public string ReplayHash { get; set; } = null!;
    public bool DefaultFilter { get; set; }
    public string CommandersTeam1 { get; set; } = null!;
    public string CommandersTeam2 { get; set; } = null!;
    [NotMapped]
    public double MmrChange { get; set; }
    [NotMapped]
    [JsonIgnore]
    public Commander[] Cmdrs1 => CommandersTeam1.Split('|', StringSplitOptions.RemoveEmptyEntries).Select(s => int.Parse(s)).Cast<Commander>().ToArray();
    [NotMapped]
    [JsonIgnore]
    public Commander[] Cmdrs2 => CommandersTeam2.Split('|', StringSplitOptions.RemoveEmptyEntries).Select(s => int.Parse(s)).Cast<Commander>().ToArray();
}

public record ReplayEventListDto
{
    public string Round { get; set; } = null!;
    public string WinnerTeam { get; set; } = null!;
    public string RunnerTeam { get; set; } = null!;
    public EventDto Event { get; init; } = null!;
}

