
using System.Text;

namespace dsstats.shared;


public record BuildRequest
{
    public RatingType RatingType { get; set; }
    public TimePeriod TimePeriod { get; set; }
    public Commander Interest { get; set; }
    public Commander Versus { get; set; }
    public int FromRating { get; set; }
    public int ToRating { get; set; }
    public List<RequestNames> PlayerNames { get; set; } = new();
    public Breakpoint Breakpoint { get; set; }
    public bool WithLeavers { get; set; }
}

public record BuildResponse
{
    public BuildCounts BuildCounts { get; set; } = new();
    public List<BuildResponseBreakpointUnit> Units { get; set; } = new();
}

public record BuildResponseBreakpointUnit
{
    public string Name { get; set; } = string.Empty;
    public double Count { get; set; }
    public double Cost { get; set; }
    public double Life { get; set; }
}

public record BuildResponseReplay
{
    public string ReplayHash { get; set; } = string.Empty;
    public DateTime Gametime { get; set; }
}

public record BuildCounts
{
    public int Count { get; init; }
    public int CmdrCount { get; init; }
    public double Winrate { get; init; }
    public double AvgGain { get; init; }
    public double Duration { get; init; }
    public double Gas { get; init; }
    public double Upgrades { get; init; }
}

public record BuildMapResponse
{
    public ReplayPlayerDto? ReplayPlayer { get; set; } = null!;
    public ReplayPlayerDto? OppReplayPlayer { get; set; } = null!;
}

public static class BuildRequestExtension
{
    public static string GenMemKey(this BuildRequest request)
    {
        StringBuilder sb = new();
        sb.Append("Build");
        sb.Append(request.RatingType);
        sb.Append(request.TimePeriod);
        sb.Append(request.Interest);
        sb.Append(request.Versus);
        sb.Append(request.FromRating);
        sb.Append(request.Breakpoint);
        sb.Append(request.ToRating);
        if (request.PlayerNames.Count > 0)
        {
            sb.Append(string.Join('|', request.PlayerNames.Select(s => $"{s.ToonId}|{s.RealmId}|{s.RegionId}")));
        }
        return sb.ToString();
    }
}