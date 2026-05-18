using dsstats.shared.DetailBuild;
using System.Text;

namespace dsstats.shared;

public enum BuildDetailsGasFilter
{
    Any = 0,
    WithGas = 1,
    WithoutGas = 2,
}

public enum BuildDetailsTeFilter
{
    All = 0,
    TE = 1,
    NonTE = 2,
}

public class BuildDetailsRequest
{
    public RatingType RatingType { get; set; } = RatingType.All;
    public TimePeriod TimePeriod { get; set; } = TimePeriod.Last12Months;
    public Commander Commander { get; set; } = Commander.None;
    public int FromRating { get; set; } = Data.MinBuildRating;
    public int ToRating { get; set; } = Data.MaxBuildRating;
    public bool WithLeavers { get; set; }
    public BuildDetailsGasFilter GasFilter { get; set; } = BuildDetailsGasFilter.Any;
    public BuildDetailsTeFilter TeFilter { get; set; } = BuildDetailsTeFilter.All;
    public PlayerDto? Player { get; set; }
}

public class BuildDetailsMatchupRequest : BuildDetailsRequest
{
    public Commander SelectedCommander { get; set; }
    public int SelectedBuild { get; set; }
}

public sealed class BuildDetailsSamplesRequest : BuildDetailsMatchupRequest
{
    public Commander OpponentCommander { get; set; }
    public int OpponentBuild { get; set; }
    public int Count { get; set; } = 10;
}

public sealed class BuildDetailsOverviewRow
{
    public Commander Commander { get; set; }
    public int Build { get; set; }
    public int Games { get; set; }
    public int Wins { get; set; }
    public double AverageRatingGain { get; set; }
    public double Winrate { get; set; }
    public double AverageRating { get; set; }
    public int GasFirstGames { get; set; }
    public double GasFirstRate { get; set; }
}

public sealed class BuildDetailsMatchupRow
{
    public Commander Commander { get; set; }
    public int Build { get; set; }
    public Commander OpponentCommander { get; set; }
    public int OpponentBuild { get; set; }
    public int Games { get; set; }
    public int Wins { get; set; }
    public double AverageRatingGain { get; set; }
    public double Winrate { get; set; }
    public double AverageRating { get; set; }
    public int SelectedGasFirstGames { get; set; }
    public int OpponentGasFirstGames { get; set; }
}

public sealed class BuildDetailsSampleReplay
{
    public ReplayListDto Replay { get; set; } = new();
    public Commander Commander { get; set; }
    public int Build { get; set; }
    public bool GasFirst { get; set; }
    public Commander OpponentCommander { get; set; }
    public int OpponentBuild { get; set; }
    public bool OpponentGasFirst { get; set; }
}

public static class BuildDetailsRequestExtensions
{
    public static string GetMemKey(this BuildDetailsRequest request)
    {
        var playerKey = request.Player?.PlayerId ?? 0;
        return $"builddetails_{request.RatingType}_{request.TimePeriod}_{request.Commander}_{request.FromRating}_{request.ToRating}_{request.WithLeavers}_{request.GasFilter}_{request.TeFilter}_{playerKey}";
    }

    public static string GetMemKey(this BuildDetailsMatchupRequest request)
    {
        return $"{((BuildDetailsRequest)request).GetMemKey()}_{request.SelectedCommander}_{request.SelectedBuild}";
    }

    public static string GetMemKey(this BuildDetailsSamplesRequest request)
    {
        return $"{((BuildDetailsMatchupRequest)request).GetMemKey()}_{request.OpponentCommander}_{request.OpponentBuild}_{request.Count}";
    }

    public static string GetBuildName(this BuildDetailsOverviewRow row)
    {
        return GetBuildName(row.Commander, row.Build);
    }

    public static string GetBuildName(this BuildDetailsMatchupRow row)
    {
        return GetBuildName(row.Commander, row.Build);
    }

    public static string GetOpponentBuildName(this BuildDetailsMatchupRow row)
    {
        return GetBuildName(row.OpponentCommander, row.OpponentBuild);
    }

    public static string GetBuildName(Commander commander, int build)
    {
        return commander switch
        {
            Commander.Protoss => Enum.IsDefined((ProtossBuild)build) ? ((ProtossBuild)build).ToString() : build.ToString(),
            Commander.Terran => Enum.IsDefined((TerranBuild)build) ? ((TerranBuild)build).ToString() : build.ToString(),
            Commander.Zerg => Enum.IsDefined((ZergBuild)build) ? ((ZergBuild)build).ToString() : build.ToString(),
            _ => build.ToString()
        };
    }

    public static string GetReplayLink(this BuildDetailsSamplesRequest request)
    {
        var sb = new StringBuilder();
        sb.Append("replays?");
        sb.Append($"PlayerCmdr={Uri.EscapeDataString(request.SelectedCommander.ToString())}");
        sb.Append($"&OppCmdr={Uri.EscapeDataString(request.OpponentCommander.ToString())}");
        if (request.Player is not null)
        {
            sb.Append($"&ToonIds={Uri.EscapeDataString(Data.GetToonIdString(request.Player.ToonId))}");
        }
        return sb.ToString();
    }
}
