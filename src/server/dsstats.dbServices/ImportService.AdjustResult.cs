using dsstats.db;
using dsstats.shared;
using dsstats.shared.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace dsstats.dbServices;

public partial class ImportService
{
    public static bool AdjustReplayResult(ReplayDto replay)
    {
        if (CheckPlayerStateVictory(replay))
        {
            return true;
        }

        if (replay.WinnerTeam != 0 || replay.Duration < 300 || replay.Players.Count < 6)
        {
            return false;
        }
        var team1KilledObjective = replay.Cannon > 0;
        var team2KilledObjective = replay.Bunker > 0;

        var team1Spawns = replay.Players.Where(p => p.TeamId == 1)
            .SelectMany(m => m.Spawns)
            .Where(x => x.Breakpoint == Breakpoint.All);

        var team2Spawns = replay.Players.Where(p => p.TeamId == 2)
            .SelectMany(m => m.Spawns)
            .Where(x => x.Breakpoint == Breakpoint.All);

        int team1TotalIncome = team1Spawns.Sum(s => s.Income);
        int team2TotalIncome = team2Spawns.Sum(s => s.Income);
        var team1Kills = team1Spawns.Sum(s => s.KilledValue);
        var team2Kills = team2Spawns.Sum(s => s.KilledValue);

        int lastTeamThatControlledMiddle = 0;
        if (replay.MiddleChanges.Count > 1)
        {
            var firstTeam = replay.MiddleChanges.First();
            var firstTeamControll = replay.MiddleChanges.Count % 2 == 0;
            if (firstTeamControll)
            {
                lastTeamThatControlledMiddle = firstTeam;
            }
            else
            {
                lastTeamThatControlledMiddle = firstTeam == 1 ? 2 : 1;
            }
        }

        ReplayResultAdjustMetrics metrics = new()
        {
            LastTeamThatControlledMiddle = lastTeamThatControlledMiddle,
            Team1 = new TeamAdjustMetrics
            {
                Income = team1TotalIncome,
                Kills = team1Kills,
                ObjectiveDestroyed = team1KilledObjective,
                ControllingMiddle = lastTeamThatControlledMiddle == 1
            },
            Team2 = new TeamAdjustMetrics
            {
                Income = team2TotalIncome,
                Kills = team2Kills,
                ObjectiveDestroyed = team2KilledObjective,
                ControllingMiddle = lastTeamThatControlledMiddle == 2
            }
        };

        int winnerTeam = TryDecideWinnerTeam(metrics);

        if (winnerTeam > 0)
        {
            SetWinnerTeam(winnerTeam, replay);
            return true;
        }
        return false;
    }

    private static bool CheckPlayerStateVictory(ReplayDto replay)
    {
        if (replay.Gametime <= new DateTime(2023, 3, 30))
        {
            return false;
        }

        var victoryPlayer = replay.Players
            .FirstOrDefault(f => f.Upgrades.Any(a => a.Name == "PlayerStateVictory"));

        if (victoryPlayer == null)
        {
            return false;
        }

        SetWinnerTeam(victoryPlayer.TeamId, replay);
        return true;
    }

    private static void SetWinnerTeam(int winnerTeam, ReplayDto replay)
    {
        replay.WinnerTeam = winnerTeam;

        foreach (var rp in replay.Players)
        {
            if (rp.TeamId == winnerTeam)
            {
                rp.Result = PlayerResult.Win;
            }
            else
            {
                rp.Result = PlayerResult.Los;
            }
        }
    }

    private static int TryDecideWinnerTeam(ReplayResultAdjustMetrics metrics)
    {
        double incomeScore = NormalizeRelative(metrics.Team1.Income, metrics.Team2.Income);
        double killsScore = NormalizeRelative(metrics.Team1.Kills, metrics.Team2.Kills);

        double objectiveScore = NormalizeBinary(
            metrics.Team1.ObjectiveDestroyed,
            metrics.Team2.ObjectiveDestroyed);

        double middleScore = NormalizeBinary(
            metrics.Team1.ControllingMiddle,
            metrics.Team2.ControllingMiddle);

        const double IncomeWeight = 0.20;
        const double KillsWeight = 0.15;
        const double ObjectiveWeight = 0.325;
        const double MiddleWeight = 0.325;

        double weightedScore =
            incomeScore * IncomeWeight +
            killsScore * KillsWeight +
            objectiveScore * ObjectiveWeight +
            middleScore * MiddleWeight;

        const double DecisionThreshold = 0.30;

        if (weightedScore >= DecisionThreshold)
            return 1;
        if (weightedScore <= -DecisionThreshold)
            return 2;

        return 0;
    }

    private static double NormalizeRelative(double v1, double v2)
    {
        if (v1 == 0 && v2 == 0) return 0;

        double diff = v1 - v2;
        double sum = Math.Abs(v1) + Math.Abs(v2);

        return diff / sum; // Range: [-1, +1]
    }

    private static double NormalizeBinary(bool t1, bool t2)
    {
        if (t1 == t2) return 0;
        return t1 ? 0.85 : -0.85;
    }

    public async Task RealWorldAdjustTest()
    {
        using var scope = scopeFactory.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();
        var replayRepository = scope.ServiceProvider.GetRequiredService<IReplayRepository>();

        var replayHashes = await context.Replays
            .Where(x => x.Gametime > new DateTime(2025, 1, 1)
                && x.PlayerCount == 6
                && x.Duration > 300
                && x.WinnerTeam > 0)
            .OrderByDescending(o => o.Gametime)
            .Select(s => s.ReplayHash)
            .Skip(1000)
            .Take(1000)
            .ToListAsync();

        int noResult = 0;
        int sameResult = 0;
        int diffResult = 0;

        foreach (var hash in replayHashes)
        {
            var details = await replayRepository.GetReplayDetails(hash);
            if (details is null)
            {
                continue;
            }

            var replay = details.Replay;
            int winnerTeam = replay.WinnerTeam;

            replay.WinnerTeam = 0;
            foreach (var player in replay.Players)
            {
                var victoryUpgrade = player.Upgrades.FirstOrDefault(f => f.Name == "PlayerStateVictory");
                if (victoryUpgrade != null)
                {
                    player.Upgrades.Remove(victoryUpgrade);
                }
            }
            var result = AdjustReplayResult(replay);
            if (!result)
            {
                noResult++;
            }
            else if (replay.WinnerTeam == winnerTeam)
            {
                sameResult++;
            }
            else if (replay.WinnerTeam != winnerTeam)
            {
                diffResult++;
            }
        }
        logger.LogWarning("Result: None: {no}, Same: {same}, Diff: {diff}", noResult, sameResult, diffResult);
    }
}

internal sealed record ReplayResultAdjustMetrics
{
    public int LastTeamThatControlledMiddle { get; init; }
    public TeamAdjustMetrics Team1 { get; init; } = new();
    public TeamAdjustMetrics Team2 { get; init; } = new();
}

internal sealed record TeamAdjustMetrics
{
    public int Income { get; init; }
    public int Kills { get; init; }
    public bool ObjectiveDestroyed { get; init; }
    public bool ControllingMiddle { get; init; }
}

