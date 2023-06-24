﻿namespace pax.dsstats.shared;

public record TimeStat
{
    public string Label { get; init; } = "";
    public Commander Commander { get; init; }
    public double Winrate { get; init; }
    public int Count { get; init; }
}

public record TimelineRequest
{
    public TimePeriod TimePeriod { get; set; }
    public RatingType RatingType { get; set; }
}

public record TimelineResponse
{
    public List<TimelineEnt> TimeLineEnts { get; set; } = new();
}

public record TimelineEnt
{
    public Commander Commander { get; set; }
    public DateTime Time { get; set; }
    public int Count { get; set; }
    public int Wins { get; set; }
    public double AvgRating { get; set; }
    public double AvgGain { get; set; }
    public double Strength { get; set; }
}