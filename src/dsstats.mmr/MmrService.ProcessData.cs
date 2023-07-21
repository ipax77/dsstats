using dsstats.mmr.ProcessData;
using pax.dsstats.shared;
using pax.dsstats.shared.Arcade;
using TeamData = dsstats.mmr.ProcessData.TeamData;

namespace dsstats.mmr;

public partial class MmrService
{
    public static void SetReplayData(Dictionary<int, CalcRating> mmrIdRatings,
                                      ReplayData replayData,
                                      Dictionary<CmdrMmrKey, CmdrMmrValue> cmdrDic,
                                      MmrOptions mmrOptions)
    {
        SetTeamData(mmrIdRatings, replayData, replayData.WinnerTeamData, cmdrDic, mmrOptions);
        SetTeamData(mmrIdRatings, replayData, replayData.LoserTeamData, cmdrDic, mmrOptions);
        SetExpectationsToWin(replayData, mmrOptions);

        replayData.Confidence = (replayData.WinnerTeamData.Confidence + replayData.LoserTeamData.Confidence) / 2;
    }

    private static void SetTeamData(Dictionary<int, CalcRating> mmrIdRatings,
                                    ReplayData replayData,
                                    TeamData teamData,
                                    Dictionary<CmdrMmrKey, CmdrMmrValue> cmdrDic,
                                    MmrOptions mmrOptions)
    {
        foreach (var playerData in teamData.Players)
        {
            SetPlayerData(mmrIdRatings, replayData, playerData, mmrOptions);
        }

        teamData.Confidence = teamData.Players.Sum(p => p.Confidence) / teamData.Players.Length;
        teamData.Mmr = teamData.Players.Sum(p => p.Mmr) / teamData.Players.Length;

        if (mmrOptions.UseCommanderMmr
            && (replayData.ReplayDsRDto.GameMode == GameMode.Commanders || replayData.ReplayDsRDto.GameMode == GameMode.CommandersHeroic))
        {
            teamData.CmdrComboMmr = GetCommandersComboMmr(replayData, teamData, cmdrDic);
        }
    }

    private static void SetExpectationsToWin(ReplayData replayData, MmrOptions mmrOptions)
    {
        double winnerPlayersExpectationToWin = EloExpectationToWin(replayData.WinnerTeamData.Mmr, replayData.LoserTeamData.Mmr, mmrOptions.Clip);
        replayData.WinnerTeamData.ExpectedResult = winnerPlayersExpectationToWin;

        if (mmrOptions.UseCommanderMmr)
        {
            double winnerCmdrExpectationToWin = EloExpectationToWin(replayData.WinnerTeamData.CmdrComboMmr, replayData.LoserTeamData.CmdrComboMmr, mmrOptions.Clip);
            replayData.WinnerTeamData.ExpectedResult = (winnerPlayersExpectationToWin + winnerCmdrExpectationToWin) / 2;
        }

        replayData.LoserTeamData.ExpectedResult = (1 - replayData.WinnerTeamData.ExpectedResult);
    }

    private static void SetPlayerData(Dictionary<int, CalcRating> mmrIdRatings,
                                      ReplayData replayData,
                                      PlayerData playerData,
                                      MmrOptions mmrOptions)
    {
        if (!mmrIdRatings.TryGetValue(playerData.MmrId, out var plRating))
        {
            plRating = mmrIdRatings[playerData.MmrId] = new CalcRating()
            {
                PlayerId = playerData.ReplayPlayer.Player.PlayerId,
                Mmr = mmrOptions.StartMmr,
                Consistency = 0,
                Confidence = 0,
                Games = 0,
            };
        }

        if (mmrOptions.InjectDic.Count > 0
            && mmrOptions.ReCalc 
            && !playerData.ReplayPlayer.IsUploader 
            && (replayData.RatingType == RatingType.Cmdr || replayData.RatingType == RatingType.Std))
        {
            plRating.Mmr = mmrOptions.GetInjectRating(replayData.RatingType,
                                                        replayData.ReplayDsRDto.GameTime,
                                                        new(playerData.ReplayPlayer.Player.ToonId,
                                                            playerData.ReplayPlayer.Player.RealmId,
                                                            playerData.ReplayPlayer.Player.RegionId));
        }

        if (BannedPlayerIds.Contains(new(playerData.ReplayPlayer.Player.ToonId,
                    playerData.ReplayPlayer.Player.RealmId,
                    playerData.ReplayPlayer.Player.RegionId)))
        {
            playerData.Mmr = mmrOptions.StartMmr;
            playerData.Consistency = 0;
            playerData.Confidence = 0;

        }
        else
        {
            playerData.Mmr = plRating.Mmr;
            playerData.Consistency = plRating.Consistency;
            playerData.Confidence = plRating.Confidence;
        }
    }

    private static IReadOnlyList<PlayerId> BannedPlayerIds = new List<PlayerId>()
    {
        new(466786, 2, 2), // SabreWolf
        new(9774911, 1, 2), // Baka
    }.AsReadOnly();
}
