
using System.Text.Json.Serialization;

namespace pax.dsstats.shared;

public record CmdrInfoRequest
{
    public RatingType RatingType { get; set; }
    public TimePeriod TimePeriod { get; set; }
    public Commander Interest { get; set; }
    public int MaxGap { get; set; }
    public int MinRating { get; set; }
    public int MaxRating { get; set; }
    public bool WithoutLeavers { get; set; }
    public int Skip { get; set; }
    public int Take { get; set; } = 20;
}

public record ReplayCmdrInfo
{
    public int ReplayId { get; set; }
    public string ReplayHash { get; set; } = string.Empty;
    public DateTime GameTime { get; set; }
    public int Duration { get; set; }
    public int Maxleaver { get; set; }
    public float Rating1 { get; set; }
    public float Rating2 { get; set; }
    public float AvgGain { get; set; }
    public string Team1 { get; set; } = string.Empty;
    public string Team2 { get; set; } = string.Empty;
    public int WinnerTeam { get; set; }
    public string Ratings { get; set; } = string.Empty;
    [JsonIgnore]
    public Commander[] Cmdrs1 => Team1.Split('|', StringSplitOptions.RemoveEmptyEntries).Select(s => int.Parse(s)).Cast<Commander>().ToArray();
    [JsonIgnore]
    public Commander[] Cmdrs2 => Team2.Split('|', StringSplitOptions.RemoveEmptyEntries).Select(s => int.Parse(s)).Cast<Commander>().ToArray();    
}

public record CmdrPlayerInfo
{
    public string Name { get; set; } = string.Empty;
    public int ToonId { get; set; }
    public int RegionId { get; set; }
    public int Count { get; set; }
    public int Wins { get; set; }
    public double AvgGain { get; set; }
    public double AvgRating { get; set; }
    [JsonIgnore]
    public int Pos { get; set; }
    [JsonIgnore]
    public double Strength { get; set; }
}
