using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace pax.dsstats.shared;

public record SynergyRequest
{
    public TimePeriod TimePeriod { get; set; }
    public RatingType RatingType { get; set; }
    public bool WithLeavers { get; set; }
    public int Exp2WinOffset { get; set; }
    public int FromRating { get; set; }
    public int ToRating { get; set; }
}

public record SynergyResponse
{
    public List<SynergyEnt> Entities { get; set; } = new();
}

public record SynergyEnt
{
    public Commander Commander { get; set; }
    public Commander Teammate { get; set; }
    public int Count { get; set; }
    public int Wins { get; set; }
    public double AvgRating { get; set; }
    public double AvgGain { get; set; }
    public double NormalizedAvgGain { get; set; }
    [JsonIgnore]
    public double Winrate => Count == 0 ? 0 : Math.Round(Wins * 100.0 / Count , 2);
}

public record CmdrSelect
{
    public Commander Commander { get; set; }
    public bool Checked { get; set; }
}