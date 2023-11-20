
using System.Text.Json.Serialization;

namespace dsstats.shared;

public record CmdrInfoRequest
{
    public RatingType RatingType { get; set; }
    public TimePeriod TimePeriod { get; set; }
    public Commander Interest { get; set; }
    public int MaxGap { get; set; }
    public int MinRating { get; set; }
    public int MaxRating { get; set; }
    public bool WithoutLeavers { get; set; }
    public bool Uploaders { get; set; }
}

public record CmdrPlayerInfo
{
    public string Name { get; set; } = string.Empty;
    public PlayerId PlayerId { get; set; } = new();
    public int Count { get; set; }
    public int Wins { get; set; }
    public double AvgGain { get; set; }
    public double AvgRating { get; set; }
    public double TeamRating { get; set; }
    [JsonIgnore]
    public int Pos { get; set; }
    [JsonIgnore]
    public double Strength { get; set; }
}