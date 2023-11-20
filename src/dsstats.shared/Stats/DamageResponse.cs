using System.Text.Json.Serialization;

namespace dsstats.shared;

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

public record WindowDimension
{
    public int Width { get; set; }
    public int Height { get; set; }
}