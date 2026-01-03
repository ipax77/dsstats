using dsstats.shared;
using dsstats.shared.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Globalization;

namespace dsstats.weblib.Replays;

public partial class ReplayComponent : ComponentBase
{
    [Inject]
    public IJSRuntime JSRuntime { get; set; } = null!;

    [Inject]
    public IPlayerService PlayerService { get; set; } = null!;

    [Parameter, EditorRequired]
    public ReplayDetails ReplayDetails { get; set; } = null!;

    [Parameter]
    public bool IsScrollable { get; set; }

    [Parameter]
    public bool IsCloseable { get; set; }
    [Parameter]
    public EventCallback<PlayerStatsResponse> OnPlayerRequest { get; set; }

    [Parameter]
    public EventCallback OnRatingUpdateRequest { get; set; }

    [Parameter]
    public EventCallback<bool> OnScrollRequest { get; set; }

    [Parameter]
    public EventCallback OnClose { get; set; }

    private ReplayHelper _replayHelper = null!;
    private ReplayMiddleChart? replayMiddleChart;
    bool showTierUps;
    bool showRefineries;
    bool showLeavers;
    private Lazy<Task<IJSObjectReference>> moduleTask = null!;

    protected override void OnInitialized()
    {
        moduleTask = new(() => JSRuntime.InvokeAsync<IJSObjectReference>(
       "import", "./_content/dsstats.weblib/js/annotationChart.js?v=0.5").AsTask());
        _replayHelper = new ReplayHelper(ReplayDetails);
        base.OnInitialized();
    }

    public void Update(ReplayDetails replayDetails)
    {
        ReplayDetails = replayDetails;
        _replayHelper = new ReplayHelper(ReplayDetails);
    }

    public string GetBreakpointString(Breakpoint bp)
    {
        if (bp == Breakpoint.All)
        {
            int totalMinutes = (int)TimeSpan.FromSeconds(ReplayDetails.Replay.Duration).TotalMinutes;
            return $"Min{totalMinutes}";
        }
        else
        {
            return bp.ToString();
        }
    }

    public void Scroll(bool left)
    {
        OnScrollRequest.InvokeAsync(left);
    }

    public void RequestBuild(ToonIdDto toonId)
    {
        var player = _replayHelper.GetPlayer(toonId);
        if (player == null)
        {
            return;
        }
        if (_replayHelper.ActiveBuilds.Contains(player))
        {
            _replayHelper.ActiveBuilds.Remove(player);
        }
        else
        {
            _replayHelper.ActiveBuilds.Add(player);
            _replayHelper.ActiveBuilds = _replayHelper.ActiveBuilds.OrderBy(o => o.GamePos).ToHashSet();
        }
    }

    private async Task RequestPlayerStats(PlayerDto player)
    {
        PlayerStatsRequest request = new()
        {
            ToonId = player.ToonId,
            Player = player,
            RatingType = _replayHelper.RatingType,
        };
        var stats = await PlayerService.GetPlayerStats(request);
        await OnPlayerRequest.InvokeAsync(stats);
    }

    public void Close()
    {
        OnClose.InvokeAsync();
    }
}

public class ReplayHelper
{
    private static readonly int min5 = Convert.ToInt32(6_720 / 22.4);
    private static readonly int min10 = Convert.ToInt32(13_440 / 22.4);
    private static readonly int min15 = Convert.ToInt32(20_160 / 22.4);

    private ReplayDetails _replayDetails;
    private int maxKills;

    public ReplayHelper(ReplayDetails replayDetails)
    {
        _replayDetails = replayDetails;
        IsTE = _replayDetails.Replay.Title.Contains("TE");
        RatingType = GetDefaultRating();
        maxKills = _replayDetails.Replay.Players
            .SelectMany(p => p.Spawns)
            .Where(s => s.Breakpoint == Breakpoint.All)
            .DefaultIfEmpty()
            .Max(m => m?.KilledValue ?? 0);
        if (_replayDetails.ReplayRatings.Count > 0)
        {
            HasRating = true;
        }
    }

    public Breakpoint Breakpoint { get; set; } = Breakpoint.All;
    public RatingType RatingType { get; set; } = RatingType.All;
    public bool ShowChart { get; set; }
    public bool ShowFileName { get; set; }
    public bool HasRating { get; set; }
    public bool IsTE { get; set; }
    public HashSet<ReplayPlayerDto> ActiveBuilds { get; set; } = [];

    private RatingType GetDefaultRating()
    {
        var availableTypes = GetRatingTypes();
        if (availableTypes.Count <= 1)
        {
            return RatingType.All;
        }
        var defaultType = _replayDetails.Replay.GameMode switch
        {
            GameMode.Standard => IsTE ? RatingType.StandardTE : RatingType.Standard,
            GameMode.Commanders => IsTE ? RatingType.CommandersTE : RatingType.Commanders,
            GameMode.CommandersHeroic => RatingType.Commanders,
            _ => RatingType.All
        };
        if (_replayDetails.ReplayRatings.Any(a => a.RatingType == defaultType))
        {
            return defaultType;
        }
        return RatingType.All;
    }

    public ReplayPlayerRatingDto? GetRating(ReplayPlayerDto replayPlayer)
    {
        return _replayDetails.ReplayRatings
            .Where(x => x.RatingType == RatingType)
            .SelectMany(s => s.ReplayPlayerRatings)
            .FirstOrDefault(f => f.ToonId == replayPlayer.Player.ToonId);
    }
    public HashSet<RatingType> GetRatingTypes()
    {
        return _replayDetails.ReplayRatings.Select(s => s.RatingType)
            .Distinct()
            .OrderBy(s => s)
            .ToHashSet();
    }

    public HashSet<Breakpoint> GetBreakpoints()
    {
        return _replayDetails.Replay.Players.SelectMany(p => p.Spawns)
            .Select(s => s.Breakpoint)
            .Distinct()
            .OrderBy(s => s)
            .ToHashSet();
    }

    public SpawnDto? GetSpawn(ToonIdDto toonId)
    {
        var player = _replayDetails.Replay.Players.FirstOrDefault(p => p.Player.ToonId == toonId);
        if (player == null)
        {
            return null;
        }
        return player.Spawns.FirstOrDefault(s => s.Breakpoint == Breakpoint)
            ?? player.Spawns.FirstOrDefault(f => f.Breakpoint == Breakpoint.All);
    }

    public SpawnDto? GetSpawn(ReplayPlayerDto player)
    {
        return player.Spawns.FirstOrDefault(s => s.Breakpoint == Breakpoint)
            ?? player.Spawns.FirstOrDefault(f => f.Breakpoint == Breakpoint.All);
    }

    public ReplayPlayerDto? GetPlayer(ToonIdDto toonId)
    {
        return _replayDetails.Replay.Players.FirstOrDefault(p => p.Player.ToonId == toonId);
    }

    public (string, string) GetExp2Win()
    {
        if (_replayDetails.ReplayRatings.Count == 0)
        {
            return (string.Empty, string.Empty);
        }
        var rating = _replayDetails.ReplayRatings
            .Where(x => x.RatingType == RatingType)
            .Select(s => s.ExpectedWinProbability)
            .FirstOrDefault();
        string colorClass;
        if (rating <= 0.5)
        {
            colorClass = "text-warning";
        }
        else
        {
            colorClass = "text-danger";
        }
        return (colorClass, rating.ToString("p0"));
    }

    public static string GetNumberString(int? num)
    {
        if (num == null)
        {
            return string.Empty;
        }
        if (num > 1000000)
        {
            return (num.Value / 1000000.0).ToString("N2") + "m";
        }
        else if (num > 1000)
        {
            return (num.Value / 1000.0).ToString("N2") + "k";
        }
        else
        {
            return num.Value.ToString("N0");
        }
    }

    public static string GetNumberString(double? num)
    {
        if (num == null)
        {
            return string.Empty;
        }
        if (num > 1000000)
        {
            return (num.Value / 1000000.0).ToString("N2") + "m";
        }
        else if (num > 1000)
        {
            return (num.Value / 1000.0).ToString("N2") + "k";
        }
        else
        {
            return num.Value.ToString("N2");
        }
    }

    public static string GetRegionString(int regionId)
    {
        return Data.GetRegionString(regionId);
    }

    public string GetAvgRating(int team)
    {
        if (_replayDetails.ReplayRatings.Count == 0)
        {
            return string.Empty;
        }

        var toonIds = _replayDetails.Replay.Players
            .Where(x => team == 1 ? x.GamePos <= 3 : x.GamePos > 3)
            .Select(s => s.Player.ToonId).ToHashSet();

        var ratings = _replayDetails.ReplayRatings
            .Where(x => x.RatingType == RatingType)
            .SelectMany(m => m.ReplayPlayerRatings.Where(x => toonIds.Contains(x.ToonId))
            .Select(s => s.RatingBefore))
            .ToList();
        if (ratings.Count == 0)
        {
            return string.Empty;
        }
        return ratings.Average().ToString("N0");
    }

    public string GetAvgIncome(List<ReplayPlayerDto> players)
    {
        return GetNumberString(players
            .SelectMany(p => p.Spawns)
            .Where(s => s.Breakpoint == Breakpoint || s.Breakpoint == Breakpoint.All)
            .Select(s => s.Income)
            .DefaultIfEmpty(0)
            .Average());
    }

    public string GetAvgArmy(List<ReplayPlayerDto> players)
    {
        return GetNumberString(players
            .SelectMany(p => p.Spawns)
            .Where(s => s.Breakpoint == Breakpoint || s.Breakpoint == Breakpoint.All)
            .Select(s => s.ArmyValue)
            .DefaultIfEmpty(0)
            .Average());
    }

    public string GetAvgKills(List<ReplayPlayerDto> players)
    {
        return GetNumberString(players
            .SelectMany(p => p.Spawns)
            .Where(s => s.Breakpoint == Breakpoint || s.Breakpoint == Breakpoint.All)
            .Select(s => s.KilledValue)
            .DefaultIfEmpty(0)
            .Average());
    }

    public string GetPlayerTableRowStyle(ReplayPlayerDto replayPlayerDto)
    {

        if (replayPlayerDto.Spawns.FirstOrDefault(f => f.Breakpoint == Breakpoint.All)?.KilledValue == maxKills)
        {
            return "background-color: #00bc8c;";
        }
        else if (replayPlayerDto.Duration < _replayDetails.Replay.Duration - 90)
        {
            var percentagePlayed = Math.Round((double)replayPlayerDto.Duration / _replayDetails.Replay.Duration * 100, 2)
                .ToString(CultureInfo.InvariantCulture);

            return $"background: linear-gradient(to right, #e74c3c {percentagePlayed}%, #e74c3cbf {percentagePlayed}%);";
        }
        else if (replayPlayerDto.IsUploader)
        {
            return "background-color: #375a7f;";
        }

        return "";
    }

    public (string, string) GetMiddleInfo()
    {
        var total = _replayDetails.Replay.Duration;
        var current = Math.Min(total, Breakpoint switch
        {
            Breakpoint.All => _replayDetails.Replay.Duration,
            Breakpoint.Min5 => min5,
            Breakpoint.Min10 => min10,
            Breakpoint.Min15 => min15,
            _ => 0
        });

        (var middle1, var middle2) = _replayDetails.Replay.GetMiddleIncome(current);
        var middle1Percent = middle1 / (double)total;
        var middle2Percent = middle2 / (double)total;
        return ($"{middle1Percent:P1}", $"{middle2Percent:P1}");
    }

    public bool IsWinner(ReplayPlayerDto player)
    {
        return player.TeamId == _replayDetails.Replay.WinnerTeam;
    }
}