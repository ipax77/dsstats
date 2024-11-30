using dsstats.razorlib.Services;
using dsstats.shared;
using Microsoft.AspNetCore.Components;
using System.Globalization;
using System.Net.NetworkInformation;

namespace dsstats.razorlib.Replays;

public partial class ReplayLog : ComponentBase
{
    [Parameter, EditorRequired]
    public ReplayDto Replay { get; set; } = default!;
    [Parameter, EditorRequired]
    public MiddleInfo MiddleInfo { get; set; } = default!;

    List<ReplayLogEvent> events = [];

    protected override void OnInitialized()
    {
        SetupLogs(Replay);
        base.OnInitialized();
    }

    private void SetupLogs(ReplayDto replay)
    {
        if (replay.Cannon > 0)
        {
            events.Add(new()
            {
                Time = TimeSpan.FromSeconds(replay.Cannon),
                Event = "Team 1 destoryed the Cannon",
                Color = "text-success"
            });
        }
        if (replay.Bunker > 0)
        {
            events.Add(new()
            {
                Time = TimeSpan.FromSeconds(replay.Bunker),
                Event = "Team 2 destoryed the Bunker",
                Color = "text-success"
            });
        }
        if (replay.WinnerTeam > 0)
        {
            events.Add(new()
            {
                Time = TimeSpan.FromSeconds(replay.Duration),
                Event = $"Team {replay.WinnerTeam} won the game",
                Color = "text-success"
            });
        }

        foreach (var player in replay.ReplayPlayers)
        {
            int i = 0;
            foreach (var refinery in player.Refineries.Split("|", StringSplitOptions.RemoveEmptyEntries))
            {
                i++;
                events.Add(new()
                {
                    Time = TimeSpan.FromSeconds(int.Parse(refinery) / 22.4),
                    Player = player,
                    InWinnerTeam = player.GamePos <= 3 && replay.WinnerTeam == 1 || player.GamePos > 3 && replay.WinnerTeam == 2,
                    Event = $"Gas {i}",
                    Color = "text-warning"
                });
            }
            i = 1;
            foreach (var tier in player.TierUpgrades.Split("|", StringSplitOptions.RemoveEmptyEntries))
            {
                i++;
                events.Add(new()
                {
                    Time = TimeSpan.FromSeconds(int.Parse(tier) / 22.4),
                    Player = player,
                    InWinnerTeam = player.GamePos <= 3 && replay.WinnerTeam == 1 || player.GamePos > 3 && replay.WinnerTeam == 2,
                    Event = $"Tier {i}",
                    Color = "text-light"
                });
            }
            if (player.Duration < replay.Duration - 90)
            {
                events.Add(new()
                {
                    Time = TimeSpan.FromSeconds(player.Duration),
                    Player = player,
                    InWinnerTeam = player.GamePos <= 3 && replay.WinnerTeam == 1 || player.GamePos > 3 && replay.WinnerTeam == 2,
                    Event = "Left the game",
                    Color = "text-danger"
                });
            }
        }

        List<Breakpoint> bps = replay.Duration switch
        {
            >= 1020 => [Breakpoint.Min5, Breakpoint.Min10, Breakpoint.Min15],
            >= 720 => [Breakpoint.Min5, Breakpoint.Min10],
            >= 420 => [Breakpoint.Min5],
            _ => [],
        };
        bps.Add(Breakpoint.All);

        foreach (var bp in bps)
        {
            TimeSpan time = bp switch
            {
                Breakpoint.Min5 => TimeSpan.FromMinutes(5),
                Breakpoint.Min10 => TimeSpan.FromMinutes(10),
                Breakpoint.Min15 => TimeSpan.FromMinutes(15),
                _ => TimeSpan.FromSeconds(replay.Duration + 1)
            };

            (var mid1, var mid2) = HelperService.GetChartMiddle(MiddleInfo, (int)time.TotalSeconds);
            var gas1 = events.Where(x => x.Player?.GamePos <= 3 && x.Time <= time && x.Event.StartsWith("Gas")).Count();
            var gas2 = events.Where(x => x.Player?.GamePos > 3 && x.Time <= time && x.Event.StartsWith("Gas")).Count();
            var spawns1 = replay.ReplayPlayers
                .Where(x => x.GamePos <= 3)
                .SelectMany(s => s.Spawns)
                .Where(x => x.Breakpoint == bp);
            var spawns2 = replay.ReplayPlayers
                .Where(x => x.GamePos > 3)
                .SelectMany(s => s.Spawns)
                .Where(x => x.Breakpoint == bp);

            events.Add(new()
            {
                Time = time,
                Sum1 = new()
                {
                    Mid = mid1,
                    GasCount = gas1,
                    ArmyValue = spawns1.Sum(s => s.ArmyValue),
                    Kills = spawns1.Sum(s => s.KilledValue)
                },
                Sum2 = new()
                {
                    Mid = mid2,
                    GasCount = gas2,
                    ArmyValue = spawns2.Sum(s => s.ArmyValue),
                    Kills = spawns2.Sum(s => s.KilledValue)
                }
            });

        }
    }
}

internal record ReplayLogEvent
{
    public TimeSpan Time { get; set; }
    public ReplayPlayerDto? Player { get; set; }
    public bool InWinnerTeam { get; set; }
    public string Event { get; set; } = string.Empty;
    public string Color { get; set; } = "text-warning";
    public ReplayBpSum? Sum1 { get; set; }
    public ReplayBpSum? Sum2 { get; set; }
}

internal record ReplayBpSum
{
    public double Mid { get; set; }
    public int GasCount { get; set; }
    public int ArmyValue { get; set; }
    public int Kills { get; set; }
}