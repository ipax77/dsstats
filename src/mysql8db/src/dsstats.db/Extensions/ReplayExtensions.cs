
using System.Security.Cryptography;
using System.Text;
using dsstats.shared;

namespace dsstats.db.Extensions;

public static class ReplayExtensions
{
    public static string GenHash(this Spawn spawn, Replay replay, ReplayPlayer replayPlayer, MD5 md5Hash)
    {
        StringBuilder sb = new();

        sb.Append(string.Join('|', replay.ReplayPlayers
            .OrderBy(o => o.GamePos)
            .Select(s => GetPlayerIdString(new(s.Player!.ToonId, s.Player.RealmId, s.Player.RegionId)))));
        sb.Append('|' + GetPlayerIdString(new(replayPlayer.Player!.ToonId, replayPlayer.Player.RealmId, replayPlayer.Player.RegionId)));
        sb.Append('|' + replayPlayer.Race);
        sb.Append('|' + replayPlayer.GamePos);
        sb.Append(replay.CommandersTeam1);
        sb.Append(replay.CommandersTeam2);
        sb.Append(spawn.Gameloop);
        sb.Append('|' + spawn.Income);
        sb.Append('|' + spawn.GasCount);
        sb.Append('|' + spawn.ArmyValue);
        sb.Append('|' + spawn.KilledValue);
        sb.Append('|' + spawn.UpgradeSpent);

        sb.Append(string.Concat(spawn.SpawnUnits
            .Select(s => $"{s.Unit!.Name}{string.Join(",", s.Positions.SelectMany(p => new[] { p.X, p.Y }))}")));

        return shared.Extensions.ReplayExtensions.GetMd5Hash(md5Hash, sb.ToString());
    }

    public static MiddleControlResult? GetMiddlePercentages(this MiddleControl? middleControl, int stepSize, int maxGameLoop)
    {
        if (middleControl is null || middleControl.Gameloops.Count == 0)
        {
            return null;
        }

        var result = new MiddleControlResult() { StepSize = stepSize };
        var gameloops = middleControl.Gameloops.Append(maxGameLoop).ToList(); // Add end of game as last point
        int currentOwner = middleControl.FirstTeam; // 1 or 2
        var controlIntervals = new List<(int start, int end, int team)>();

        // Build control intervals
        for (int i = 0; i < gameloops.Count - 1; i++)
        {
            int start = gameloops[i];
            int end = gameloops[i + 1];
            controlIntervals.Add((start, end, currentOwner));
            currentOwner = 3 - currentOwner; // swap between 1 and 2
        }

        // Now compute for each step
        for (int currentLoop = 0; currentLoop <= maxGameLoop; currentLoop += stepSize)
        {
            double currentTime = currentLoop / 22.4;
            double team1Time = 0;
            double team2Time = 0;

            foreach (var (start, end, team) in controlIntervals)
            {
                if (currentLoop < start)
                    break; // This control period hasn't started yet

                double controlStart = start / 22.4;
                double controlEnd = Math.Min(end, currentLoop) / 22.4;

                if (controlEnd > controlStart)
                {
                    double heldTime = controlEnd - controlStart;
                    if (team == 1)
                        team1Time += heldTime;
                    else
                        team2Time += heldTime;
                }
            }

            double totalTime = team1Time + team2Time;
            if (totalTime == 0)
            {
                result.Team1.Add(0);
                result.Team2.Add(0);
            }
            else
            {
                result.Team1.Add(team1Time / totalTime);
                result.Team2.Add(team2Time / totalTime);
            }
        }

        return result;
    }

    private static string? GetPlayerIdString(PlayerId? playerId)
    {
        if (playerId is null)
        {
            return null;
        }

        return $"{playerId.ToonId}|{playerId.RealmId}|{playerId.RegionId}";
    }
}

public sealed record MiddleControlResult
{
    public int StepSize { get; init; }
    public List<double> Team1 { get; init; } = [];
    public List<double> Team2 { get; init; } = [];
}