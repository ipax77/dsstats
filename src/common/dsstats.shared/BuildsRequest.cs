using System.Text;

namespace dsstats.shared;

public class BuildsRequest
{
    public RatingType RatingType { get; set; }
    public TimePeriod TimePeriod { get; set; }
    public Commander Interest { get; set; }
    public Commander Versus { get; set; }
    public int FromRating { get; set; }
    public int ToRating { get; set; }
    public Breakpoint Breakpoint { get; set; }
    public bool WithLeavers { get; set; }
    public bool WithSpawnInfo { get; set; } = true;
    public List<PlayerDto> Players { get; set; } = [];
}

public class BuildsResponse
{
    public BuildStats Stats { get; set; } = new();
    public List<BuildUnit> Units { get; set; } = [];
    public List<ReplayListDto> Replays { get; set; } = [];
    public ReplayDto? SampleReplay { get; set; }
}

public class BuildStats
{
    public int Count { get; init; }
    public int CmdrCount { get; init; }
    public double Winrate { get; init; }
    public double AvgGain { get; init; }
    public double Duration { get; init; }
    public double Gas { get; init; }
    public double Upgrades { get; init; }
}

public class BuildUnit
{
    public string Name { get; set; } = string.Empty;
    public double Count { get; set; }
    public double Cost { get; set; }
    public double Life { get; set; }
}

public static class BuildRequestExtensions
{
    public static string GetMemKey(this BuildsRequest request)
    {
        var playersKey = string.Join("_", request.Players.Select(p => $"{p.PlayerId}"));
        return $"build_{request.RatingType}_{request.TimePeriod}_{request.Interest}_{request.Versus}_{request.FromRating}_{request.ToRating}_{request.Breakpoint}_{request.WithLeavers}_{playersKey}";
    }

    public static string GetReplayLink(this BuildsRequest request)
    {
        var sb = new StringBuilder();
        sb.Append("replays?");

        // Required: PlayerCmdr
        sb.Append($"PlayerCmdr={Uri.EscapeDataString(request.Interest.ToString())}");

        // Optional: OppCmdr
        if (request.Versus != Commander.None)
        {
            sb.Append($"&OppCmdr={Uri.EscapeDataString(request.Versus.ToString())}");
        }

        // Optional: ToonIds
        if (request.Players.Count > 0)
        {
            var toonIds = request.Players
                .Take(Data.MaxBuildPlayers)
                .Select(p => Data.GetToonIdString(p.ToonId))
                .Select(Uri.EscapeDataString);

            sb.Append($"&ToonIds={string.Join("|", toonIds)}");
        }

        return sb.ToString();
    }
}