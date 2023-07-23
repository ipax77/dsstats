
using Microsoft.EntityFrameworkCore;

namespace pax.dsstats.dbng.Services;

public partial class CheatDetectService
{
    private static readonly int upgradeIdGameOver = 505;
    private static readonly int updadeIdVictory = 504;

    public async Task<CheatResult> AdjustReplays(DateTime fromDate)
    {
        var replays = await context.Replays
            .Include(i => i.ReplayPlayers)
                .ThenInclude(i => i.Player)
                    .ThenInclude(i => i.Uploader)
            .Include(i => i.ReplayPlayers)
                .ThenInclude(i => i.Upgrades)
            .Where(x => x.GameTime >= fromDate
              && (x.WinnerTeam == 0 || x.ResultCorrected)
              && x.Playercount == 6
              && x.Duration >= 60)
            .ToListAsync();
        
        CheatResult cheatResult = new() { NoResultGames = replays.Count };

        foreach (var replay in replays)
        {
            if (await AdjustReplay(context, replay, cheatResult, true))
            {
                cheatResult.DcGames++;
            }
            else
            {
                cheatResult.UnknownGames++;
            }
        }

        await context.SaveChangesAsync();
        return cheatResult;
    }

    public static async Task<bool> AdjustReplay(ReplayContext context, Replay replay, CheatResult cheatResult, bool init = false)
    {
        if (!init &&
            (replay.WinnerTeam != 0
            || replay.Playercount != 6
            || replay.Duration < 60))
        {
            return false;
        }

        if (CheckPlayerStateVictory(replay))
        {
            return true;
        }
        else if (init)
        {
            return false;
        }

        if (!init)
        {
            await LoadMissingReplayData(context, replay);
        }

        var uploaderReplayPlayer = GetUploaderReplayPlayer(replay);

        if (uploaderReplayPlayer == null)
        {
            cheatResult.UnknownGames++;
            return false;
        }

        int rqScore = GetRageQuitScore(replay, uploaderReplayPlayer.Team);

        if (rqScore > 0)
        {
            cheatResult.RqGames++;
            uploaderReplayPlayer.Player.RageQuitCount++;
            CorrectResult(replay, uploaderReplayPlayer.Team == 1 ? 2 : 1);
        }
        else
        {
            cheatResult.DcGames++;
            uploaderReplayPlayer.Player.DisconnectCount++;
        }
        return true;
    }

    private static bool CheckPlayerStateVictory(Replay replay)
    {
        if (replay.GameTime <= new DateTime(2023, 3, 30))
        {
            return false;
        }

        int team1Win = 0;
        int team2Win = 0;
        foreach (var replayPlayer in replay.ReplayPlayers)
        {
            var gameOver = replayPlayer.Upgrades
                .FirstOrDefault(f => f.UpgradeId == upgradeIdGameOver);

            if (gameOver == null)
            {
                continue;
            }

            var victory = replayPlayer.Upgrades
                .FirstOrDefault(f => f.UpgradeId == updadeIdVictory);

            if (victory != null)
            {
                if (replayPlayer.Team == 1)
                {
                    team1Win++;
                }
                else if (replayPlayer.Team == 2)
                {
                    team2Win++;
                }
            }
        }

        if (team1Win == 0 && team2Win == 0)
        {
            return false;
        }
        else
        {
            if (team1Win > team2Win)
            {
                CorrectResult(replay, 1);
            }
            else if (team2Win > team1Win)
            {
                CorrectResult(replay, 2);
            }
            else
            {
                return false;
            }
            return true;
        }
    }

    private static void CorrectResult(Replay replay, int winnerTeam)
    {
        foreach (var replayPlayer in replay.ReplayPlayers)
        {
            if (replayPlayer.Team == winnerTeam)
            {
                replayPlayer.PlayerResult = shared.PlayerResult.Win;
            }
            else
            {
                replayPlayer.PlayerResult = shared.PlayerResult.Los;
            }
        }
        replay.WinnerTeam = winnerTeam;
        replay.ResultCorrected = true;
    }

    private static ReplayPlayer? GetUploaderReplayPlayer(Replay replay)
    {
        var uploaders = replay.ReplayPlayers.Where(x => x.IsUploader).ToList();

        if (!uploaders.Any() || uploaders.Count > 1)
        {
            return null;
        }
        return uploaders.First();
    }

    private static async Task LoadMissingReplayData(ReplayContext context, Replay replay)
    {
        var playerIds = replay.ReplayPlayers
            .Select(s => s.PlayerId)
            .Distinct().ToList();

        await context.Players
            .Where(x => playerIds.Contains(x.PlayerId))
            .LoadAsync();

        var uploaderIds = replay.ReplayPlayers
            .Select(s => s.Player.UploaderId)
            .Distinct().ToList();

#pragma warning disable CS8629 // Nullable value type may be null.
        List<int> uploadersIdsNoNull = uploaderIds
            .Where(x => x != null)
            .Select(s => s.Value)
            .ToList();
#pragma warning restore CS8629 // Nullable value type may be null.

        await context.Uploaders
            .Where(x => uploadersIdsNoNull.Contains(x.UploaderId))
            .LoadAsync();
    }


    private static int GetRageQuitScore(Replay replay, int uploaderTeam)
    {
        int rqScore = 0;

        var rqData = GetRageQuitData(replay, uploaderTeam);

        rqScore += GetNumberScore(rqData.IncomeDiff);
        rqScore += GetNumberScore(rqData.ArmyDiff);
        rqScore += GetNumberScore(rqData.KillsDiff);

        rqScore += GetMiddleScore(rqData.UploaderLastMiddleHoldDuration);

        return rqScore;
    }

    private static ReplayRageQuitData GetRageQuitData(Replay replay, int uploaderTeam)
    {
        var incomeDiff = replay.ReplayPlayers.Where(x => x.Team == 1).Sum(s => s.Income)
                        - replay.ReplayPlayers.Where(x => x.Team == 2).Sum(s => s.Income);

        var armyDiff = replay.ReplayPlayers.Where(x => x.Team == 1).Sum(s => s.Army)
                        - replay.ReplayPlayers.Where(x => x.Team == 2).Sum(s => s.Army);

        var killsDiff = replay.ReplayPlayers.Where(x => x.Team == 1).Sum(s => s.Kills)
                        - replay.ReplayPlayers.Where(x => x.Team == 2).Sum(s => s.Kills);

        var uploaderLastMiddleHoldDuration = GetUploaderLastMiddleHold(replay.Middle, replay.Duration, uploaderTeam);

        if (uploaderTeam != 1)
        {
            incomeDiff *= -1;
            armyDiff *= -1;
            killsDiff *= -1;
        }

        return new()
        {
            IncomeDiff = incomeDiff,
            ArmyDiff = armyDiff,
            KillsDiff = killsDiff,
            UploaderLastMiddleHoldDuration = uploaderLastMiddleHoldDuration
        };
    }

}

internal record ReplayRageQuitData
{
    public int IncomeDiff { get; init; }
    public int ArmyDiff { get; init; }
    public int KillsDiff { get; init; }
    public int UploaderLastMiddleHoldDuration { get; init; }
}