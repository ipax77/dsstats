using dsstats.shared;
using dsstats.shared.Extensions;

namespace dsstats.db8services.Import;

public partial class ImportService
{
    public static void AdjustReplay(ReplayDto replay)
    {
        if (replay.WinnerTeam != 0
            || replay.Playercount != 6
            || replay.Duration < 60)
        {
            return;
        }

        if (CheckPlayerStateVictory(replay))
        {
            return;
        }

        var rqData = GetRageQuitData(replay);

        var rqScore1 = GetRageQuitScoreTeam1(rqData);
        var rqScore2 = GetRageQuitScoreTeam2(rqData);

        int winnerTeam = 0;
        if (rqScore1 > rqScore2 + 1)
        {
            winnerTeam = 2;
        }

        if (rqScore2 > rqScore1 + 1)
        {
            winnerTeam = 1;
        }

        if (winnerTeam != 0)
        {
            SetWinnerTeam(winnerTeam, replay);
        }
    }

    private static int GetRageQuitScoreTeam1(ReplayRageQuitData rqData)
    {
        int rqScore = 0;

        if (rqData.IncomeDiff < 0)
        {
            rqScore++;
        }

        if (rqData.ArmyDiff < 0)
        {
            rqScore++;
        }

        if (rqData.KillsDiff < 0)
        {
            rqScore++;
        }

        if (rqData.Bunker)
        {
            rqScore++;
        }

        if (rqData.LastMiddleTeam == 2)
        {
            rqScore++;
            if (rqData.LastMiddleHold > 90)
            {
                rqScore++;
            }
        }
        return rqScore;
    }

    private static int GetRageQuitScoreTeam2(ReplayRageQuitData rqData)
    {
        int rqScore = 0;

        if (rqData.IncomeDiff > 0)
        {
            rqScore++;
        }

        if (rqData.ArmyDiff > 0)
        {
            rqScore++;
        }

        if (rqData.KillsDiff > 0)
        {
            rqScore++;
        }

        if (rqData.Cannon)
        {
            rqScore++;
        }

        if (rqData.LastMiddleTeam == 1)
        {
            rqScore++;
            if (rqData.LastMiddleHold > 90)
            {
                rqScore++;
            }
        }
        return rqScore;
    }


    private static ReplayRageQuitData GetRageQuitData(ReplayDto replay)
    {
        var armyDiff = replay.ReplayPlayers.Where(x => x.Team == 1).Sum(s => s.Army)
                        - replay.ReplayPlayers.Where(x => x.Team == 2).Sum(s => s.Army);

        var killsDiff = replay.ReplayPlayers.Where(x => x.Team == 1).Sum(s => s.Kills)
                        - replay.ReplayPlayers.Where(x => x.Team == 2).Sum(s => s.Kills);

        var middleInfo = replay.GetMiddleInfo();
        (var lastHoldTeam, var lastHoldDuration) = GetLastMiddleTeamAndHold(middleInfo);

        return new()
        {
            IncomeDiff = middleInfo.Team1Income - middleInfo.Team2Income,
            ArmyDiff = armyDiff,
            KillsDiff = killsDiff,
            LastMiddleTeam = lastHoldTeam,
            LastMiddleHold = lastHoldDuration
        };
    }

    private static (int, int) GetLastMiddleTeamAndHold(MiddleInfo middleInfo)
    {
        if (middleInfo.MiddleChanges.Count == 0)
        {
            return (0, 0);
        }

        int lastHoldTeam = middleInfo.MiddleChanges.Count % 2 != 0 ?
            middleInfo.StartTeam : middleInfo.StartTeam == 1 ? 2 : 1;

        int lastHoldDuration = Convert.ToInt32(middleInfo.Duration - middleInfo.MiddleChanges.Last());

        return (lastHoldTeam, lastHoldDuration);
    }

    private static bool CheckPlayerStateVictory(ReplayDto replay)
    {
        if (replay.GameTime <= new DateTime(2023, 3, 30))
        {
            return false;
        }

        var victoryPlayer = replay.ReplayPlayers
            .FirstOrDefault(f => f.Upgrades.Any(a => a.Upgrade.Name == "PlayerStateVictory"));

        if (victoryPlayer == null)
        {
            return false;
        }

        SetWinnerTeam(victoryPlayer.Team, replay);
        return true;
    }


    private static void SetWinnerTeam(int winnerTeam, ReplayDto replay)
    {
        replay.WinnerTeam = winnerTeam;

        foreach (var rp in replay.ReplayPlayers)
        {
            if (rp.Team == winnerTeam)
            {
                rp.PlayerResult = PlayerResult.Win;
            }
            else
            {
                rp.PlayerResult = PlayerResult.Los;
            }
        }
    }
}


internal record ReplayRageQuitData
{
    public int IncomeDiff { get; init; }
    public int ArmyDiff { get; init; }
    public int KillsDiff { get; init; }
    public bool Cannon { get; init; }
    public bool Bunker { get; init; }
    public int LastMiddleTeam { get; init; }
    public int LastMiddleHold { get; init; }
}