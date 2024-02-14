﻿using System.Text.Json.Serialization;

namespace dsstats.shared;

public record TourneysStatsResponse
{
    public TourneysStatsRequest Request { get; init; } = new();
    public List<TourneysStatsResponseItem> Items { get; init; } = new();
    public TourneysCountResponse CountResponse { get; init; } = new();
    public int Count { get; init; }
    public int Bans { get; set; }
    public int AvgDuration { get; init; }
}


public record TourneysStatsResponseItem
{
    public string Label { get; init; } = null!;
    public int Matchups { get; init; }
    public int Wins { get; init; }
    [JsonIgnore]
    public long duration { get; init; }
    public int Replays { get; init; }
    public int Bans { get; set; }
    [JsonIgnore]
    public double Winrate => Matchups == 0 ? 0 : Wins * 100 / (double)Matchups;
    [JsonIgnore]
    public Commander Cmdr => Enum.TryParse(typeof(Commander), Label, out _) ? (Commander)Enum.Parse(typeof(Commander), Label) : Commander.None;
}

public record TourneysCountResponse
{
    public int Count { get; init; }
    public int DefaultFilter { get; init; }
    public int Leaver { get; init; }
    public int Quits { get; init; }
}

public record TourneysStatsRequest
{
    public TimePeriod TimePeriod { get; set; } = TimePeriod.Past90Days;
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

public record TourneyStatsRequest
{
    public Guid TourneyGuid { get; set; }
}

public record TourneyStatsResponse
{
    public int Players { get; set; }
    public int Matches { get; set; }
    public int Teams { get; set; }
    public List<TourneyCommanderStat> CommanderStats { get; set; } = [];

}

public record TourneyCommanderStat
{
    public Commander Commander { get; init; }
    public int Count { get; init; }
    public int Wins { get; init; }
    public int Bans { get; set; }
}

public record TourneyCommanderTableStat : TourneyCommanderStat
{
    public TourneyCommanderTableStat(TourneyCommanderStat stat)
    {
        Commander = stat.Commander;
        Count = stat.Count;
        Wins = stat.Wins;
        Bans = stat.Bans;
        Winrate = Count == 0 ? 0 : Math.Round(Wins * 100.0 / (double)Count, 2);
    }
    public double Winrate { get; init; }
}
