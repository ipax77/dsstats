namespace dsstats.shared;

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
}

public static class SynergyResponseExtensions
{
    public static double Winrate(this SynergyEnt synergyEnt)
    {
        return synergyEnt.Count == 0 ? 0
            : Math.Round(synergyEnt.Wins * 100.0 / synergyEnt.Count, 2);
    }
}

public record GroupSynergyResponse : SynergyResponse
{
    public List<Commander> Group { get; set; } = [];
}