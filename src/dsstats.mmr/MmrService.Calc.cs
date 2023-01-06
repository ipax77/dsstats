using dsstats.mmr.Extensions;
using dsstats.mmr.ProcessData;
using pax.dsstats.shared;
using pax.dsstats.shared.Raven;

using TeamData = dsstats.mmr.ProcessData.TeamData;

namespace dsstats.mmr;

public static partial class MmrService
{
    //# Calculate
    private static void CalculateRatingsDeltas(Dictionary<int, CalcRating> mmrIdRatings,
                                               ReplayData replayData,
                                               TeamData teamData,
                                               MmrOptions mmrOptions)
    {
        foreach (var playerData in teamData.Players)
        {
            var lastPlRating = mmrIdRatings[playerData.MmrId];

            double playerConsistency = lastPlRating.Consistency;
            double playerConfidence = lastPlRating.Confidence;
            double playerMmr = lastPlRating.Mmr;

            double factor_playerToTeamMates = PlayerToTeamMates(teamData.Mmr, playerMmr, teamData.Players.Length);
            double factor_consistency = GetCorrectedRevConsistency(1 - playerConsistency);
            double factor_confidence = GetCorrectedConfidenceFactor(playerConfidence, replayData.Confidence);

            double playerImpact = 1
                * (mmrOptions.UseFactorToTeamMates ? factor_playerToTeamMates : 1.0)
                * (mmrOptions.UseConsistency ? factor_consistency : 1.0)
                * (mmrOptions.UseConfidence ? factor_confidence : 1.0);

            playerData.Deltas.Mmr = CalculateMmrDelta(replayData.WinnerTeamData.ExpectedResult, playerImpact, mmrOptions.EloK);
            //playerData.Deltas.Consistency = MmrOptions.consistencyDeltaMult * 2 * (replayData.WinnerTeamData.ExpectedResult - 0.50);
            playerData.Deltas.Consistency = Math.Abs(teamData.ExpectedResult - teamData.ActualResult) < 0.50 ? 1 : 0;
            playerData.Deltas.Confidence = 1 - Math.Abs(teamData.ExpectedResult - teamData.ActualResult);

            if (mmrOptions.UseCommanderMmr)
            {
                var commandersMmrImpact = (playerMmr / mmrOptions.StartMmr) * playerConfidence;
                playerData.Deltas.CommanderMmr = CalculateMmrDelta(replayData.WinnerTeamData.ExpectedResult, commandersMmrImpact, mmrOptions.EloK);
            }

            if (playerData.IsLeaver)
            {
                playerData.Deltas.Consistency = 0;
                playerData.Deltas.Confidence = 0;

                playerData.Deltas.Mmr *= -1;
                playerData.Deltas.CommanderMmr = 0;
            }
            else if (!teamData.IsWinner)
            {
                playerData.Deltas.Mmr *= -1;
                playerData.Deltas.CommanderMmr *= -1;
            }

            //if (Math.Abs(playerData.Deltas.Mmr) > mmrOptions.EloK * teamData.Players.Length)
            //{
            //    // todo: no Exceptions prefered.
            //    throw new Exception("MmrDelta is bigger than eloK");
            //}
        }
    }

    private static void FixMmrEquality(TeamData teamData, TeamData oppTeamData)
    {
        PlayerData[] posDeltaPlayers =
            teamData.Players.Where(x => x.Deltas.Mmr >= 0).Concat(
            oppTeamData.Players.Where(x => x.Deltas.Mmr >= 0)).ToArray();

        PlayerData[] negPlayerDeltas =
            teamData.Players.Where(x => x.Deltas.Mmr < 0).Concat(
            oppTeamData.Players.Where(x => x.Deltas.Mmr < 0)).ToArray();

        double absSumPosDeltas = Math.Abs(teamData.Players.Sum(x => Math.Max(0, x.Deltas.Mmr)) + oppTeamData.Players.Sum(x => Math.Max(0, x.Deltas.Mmr)));
        double absSumNegDeltas = Math.Abs(teamData.Players.Sum(x => Math.Min(0, x.Deltas.Mmr)) + oppTeamData.Players.Sum(x => Math.Min(0, x.Deltas.Mmr)));
        double absSumAllDeltas = absSumPosDeltas + absSumNegDeltas;

        //if (teamData.Players.Length != oppTeamData.Players.Length)
        //{
        //    throw new Exception("Not same player amount.");
        //}

        if (absSumPosDeltas == 0 || absSumNegDeltas == 0) {
            foreach (var player in teamData.Players) {
                player.Deltas.Mmr = 0;
            }
            foreach (var player in oppTeamData.Players) {
                player.Deltas.Mmr = 0;
            }
            return;
        }

        foreach (var posDeltaPlayer in posDeltaPlayers) {
            posDeltaPlayer.Deltas.Mmr *= (absSumAllDeltas / (absSumPosDeltas * 2));
        }
        foreach (var negDeltaPlayer in negPlayerDeltas) {
            negDeltaPlayer.Deltas.Mmr *= (absSumAllDeltas / (absSumNegDeltas * 2));
        }
    }

    private static List<PlChange> AddPlayersRankings(Dictionary<int, CalcRating> mmrIdRatings,
                                                                  TeamData teamData,
                                                                  DateTime gameTime,
                                                                  int maxKills)
    {
        List<PlChange> changes = new();
        foreach (var player in teamData.Players) {
            var currentPlayerRating = mmrIdRatings[player.MmrId];

            double mmrBefore = currentPlayerRating.Mmr;
            double consistencyBefore = currentPlayerRating.Consistency;
            double confidenceBefore = currentPlayerRating.Confidence;

            double mmrAfter = mmrBefore + player.Deltas.Mmr;
            const double consistencyBeforePercentage = 0.99;
            double consistencyAfter = ((consistencyBefore * consistencyBeforePercentage) + (player.Deltas.Consistency * (1 - consistencyBeforePercentage)));
            const double confidenceBeforePercentage = 0.99;
            double confidenceAfter = ((confidenceBefore * confidenceBeforePercentage) + (player.Deltas.Confidence * (1 - confidenceBeforePercentage)));

            consistencyAfter = Math.Clamp(consistencyAfter, 0, 1);
            confidenceAfter = Math.Clamp(confidenceAfter, 0, 1);
            //mmrAfter = Math.Max(1, mmrAfter);

            changes.Add(new PlChange() { Pos = player.GamePos, ReplayPlayerId = player.ReplayPlayerId, Change = mmrAfter - mmrBefore });

            currentPlayerRating.Consistency = consistencyAfter;
            currentPlayerRating.Confidence = confidenceAfter;
            currentPlayerRating.Games++;

            if (player.PlayerResult == PlayerResult.Win) {
                currentPlayerRating.Wins++;
            }
            if (player.Kills == maxKills) {
                currentPlayerRating.Mvp++;
            }
            if (player.IsUploader) {
                currentPlayerRating.IsUploader = true;
            }

            currentPlayerRating.SetCmdr(player.Race);
            currentPlayerRating.SetMmr(mmrAfter, gameTime);
        }
        return changes;
    }

    //# Set ProcessData
    private static void SetReplayData(Dictionary<int, CalcRating> mmrIdRatings,
                                      ReplayData replayData,
                                      Dictionary<CmdrMmmrKey, CmdrMmmrValue> cmdrDic,
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
                                    Dictionary<CmdrMmmrKey, CmdrMmmrValue> cmdrDic,
                                    MmrOptions mmrOptions)
    {
        foreach (var playerData in teamData.Players)
        {
            SetPlayerData(mmrIdRatings, playerData, mmrOptions);
        }

        teamData.Confidence = teamData.Players.Sum(p => p.Confidence) / teamData.Players.Length;
        teamData.Mmr = teamData.Players.Sum(p => p.Mmr) / teamData.Players.Length;

        if (mmrOptions.UseCommanderMmr && !replayData.IsStd)
        {
            teamData.CmdrComboMmr = GetCommandersComboMmr(replayData, teamData, cmdrDic);
        }
    }

    private static void SetExpectationsToWin(ReplayData replayData, MmrOptions mmrOptions)
    {
        double winnerPlayersExpectationToWin = EloExpectationToWin(replayData.WinnerTeamData.Mmr, replayData.LoserTeamData.Mmr, mmrOptions.Clip);
        replayData.WinnerTeamData.ExpectedResult = winnerPlayersExpectationToWin;

        if (mmrOptions.UseCommanderMmr) {
            double winnerCmdrExpectationToWin = EloExpectationToWin(replayData.WinnerTeamData.CmdrComboMmr, replayData.LoserTeamData.CmdrComboMmr, mmrOptions.Clip);
            replayData.WinnerTeamData.ExpectedResult = (winnerPlayersExpectationToWin + winnerCmdrExpectationToWin) / 2;
        }

        replayData.LoserTeamData.ExpectedResult = (1 - replayData.WinnerTeamData.ExpectedResult);
    }

    private static void SetPlayerData(Dictionary<int, CalcRating> mmrIdRatings, PlayerData playerData, MmrOptions mmrOptions)
    {
        if (!mmrIdRatings.TryGetValue(playerData.MmrId, out var plRating))
        {
            plRating = mmrIdRatings[playerData.MmrId] = new CalcRating()
            {
                PlayerId = playerData.PlayerId,
                Mmr = mmrOptions.StartMmr,
                Consistency = 0,
                Confidence = 0,
                Games = 0,
            };
        }

        playerData.Mmr = plRating.Mmr;
        playerData.Consistency = plRating.Consistency;
        playerData.Confidence = plRating.Confidence;
    }
}
