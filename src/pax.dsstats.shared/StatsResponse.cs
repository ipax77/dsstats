using System.Text.Json.Serialization;

namespace pax.dsstats.shared;

public record StatsResponse
{
    public StatsRequest Request { get; init; } = new();
    public ICollection<StatsResponseItem> Items { get; init; } = new List<StatsResponseItem>();
    public CountResponse CountResponse { get; init; } = new();
    public int Count { get; init; }
    public int Bans { get; set; }
    public int AvgDuration { get; init; }
}


public record StatsResponseItem
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

public record CountResponse
{
    public int Count { get; init; }
    public int DefaultFilter { get; init; }
    public int Leaver { get; init; }
    public int Quits { get; init; }
}
