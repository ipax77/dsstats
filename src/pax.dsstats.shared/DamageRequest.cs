using System.Text.Json.Serialization;

namespace pax.dsstats.shared;

public record DamageRequest
{
    public TimePeriod TimePeriod { get; set; }
    public RatingType RatingType { get; set; }
    public Commander Interest { get; set; }
    public Breakpoint Breakpoint { get; set; }
    public DamageChartType ChartType { get; set; }
    public bool WithLeavers { get; set; }
    public int Exp2WinOffset { get; set; }
    public int FromRating { get; set; }
    public int ToRating { get; set; }
}

public record DamageResponse
{
    public List<DamageEnt> Entities { get; set; } = new();
}

public record DamageEnt
{
    public Commander Commander { get; set; }
    public Breakpoint Breakpoint { get; set; }
    public int Count { get; set; }
    public int Mvp { get; set; }
    public int AvgKills { get; set; }
    public int AvgArmy { get; set; }
    public int AvgUpgrades { get; set; }
    public double AvgGas { get; set; }
    public int AvgIncome { get; set; }
    public int AvgAPM { get; set; }
    [JsonIgnore]
    public double MvpPercentage => Count == 0 ? 0 : Math.Round(Mvp * 100.0 / Count, 2);
    [JsonIgnore]
    public int ArmyValue => AvgArmy + AvgUpgrades;
}

public enum DamageChartType
{
    Damage = 0,
    MVP = 1,
    Army = 2,
}

public record WindowDimension
{
    public int Width { get; set; }
    public int Height { get; set; }
}