using dsstats.db;
using dsstats.shared;

namespace dsstats.dbServices;

public partial class ImportService
{
    public static void AdjustReplayResult(Replay replay)
    {
        if (replay.WinnerTeam != 0 || replay.Duration < 300 || replay.PlayerCount < 6)
        {
            return;
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
        if (replay.MiddleChanges.Length > 1)
        {
            var firstTeam = replay.MiddleChanges.First();
            var firstTeamControll = replay.MiddleChanges.Length % 2 == 0;
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

        // decide most likley winner team

        if (winnerTeam > 0)
        {
            replay.WinnerTeam = winnerTeam;
            foreach (var player in replay.Players)
            {
                if (player.TeamId == winnerTeam)
                {
                    player.Result = PlayerResult.Win;
                }
                else
                {
                    player.Result = PlayerResult.Los;
                }
            }
        }
    }

    private static int TryDecideWinnerTeam(ReplayResultAdjustMetrics metrics)
    {
        const double IncomeWeight = 0.1;
        const double KillsWeight = 0.2;
        const double ObjectiveWeight = 0.3;
        const double MiddleWeight = 0.4;

        // 2. Define the Certainty Threshold
        // 0.5 is a tie. 0.6 means one team is 20% stronger than the other.
        const double WinningThreshold = 0.60;

        // 3. Normalize Metrics (Scale 0 to 1)
        double nIncome1 = Normalize(metrics.Team1.Income, metrics.Team2.Income);
        double nArmy1 = Normalize(metrics.Team1.Kills, metrics.Team2.Kills);
        double nObj1 = Normalize(metrics.Team1.ObjectiveDestroyed ? 1 : 0, metrics.Team2.ObjectiveDestroyed ? 1 : 0);
        double nMid1 = Normalize(metrics.Team1.ControllingMiddle ? 1 : 0, metrics.Team2.ControllingMiddle ? 1 : 0);

        // 4. Calculate Weighted Probability for Team 1
        double team1Probability = (nIncome1 * IncomeWeight) +
                                  (nArmy1 * KillsWeight) +
                                  (nObj1 * ObjectiveWeight) +
                                  (nMid1 * MiddleWeight);

        // 5. Decision
        if (team1Probability >= WinningThreshold)
            return 1;

        if (team1Probability <= (1.0 - WinningThreshold))
            return 2;

        return 0; // Result is too close to 0.5 to call
    }

    private static double Normalize(double val1, double val2)
    {
        if (val1 + val2 == 0) return 0.5; // If both are 0, they are equal
        return val1 / (val1 + val2);
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