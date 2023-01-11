
using dsstats.mmr.Extensions;
using dsstats.mmr.ProcessData;
using pax.dsstats.shared;
using pax.dsstats;
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
            var playerImpact = GetPlayerImpact(teamData, replayData, lastPlRating, mmrOptions);

            if (!playerData.IsLeaver)
            {
                playerImpact *= replayData.LeaverImpact;

                SetPlayerDeltas(playerData, teamData, replayData, playerImpact, mmrOptions);

                if (replayData.Maxleaver < 90 && mmrOptions.UseCommanderMmr)
                {
                    var commandersMmrImpact = (lastPlRating.Mmr / mmrOptions.StartMmr) * lastPlRating.Confidence;
                    playerData.Deltas.CommanderMmr = CalculateMmrDelta(replayData.WinnerTeamData.ExpectedResult, commandersMmrImpact, mmrOptions.EloK);
                }

                if (!teamData.IsWinner)
                {
                    playerData.Deltas.Mmr *= -1;
                    playerData.Deltas.CommanderMmr *= -1;
                }
            }
            else
            {
                SetLeaverPlayerDeltas(playerData, replayData, playerImpact, mmrOptions);
            }

            //if (Math.Abs(playerData.Deltas.Mmr) > mmrOptions.EloK * teamData.Players.Length)
            //{
            //    // todo: no Exceptions prefered.
            //    throw new Exception("MmrDelta is bigger than eloK");
            //}
        }
    }

    private static void SetPlayerDeltas(PlayerData playerData, TeamData teamData, ReplayData replayData, double playerImpact, MmrOptions mmrOptions)
    {
        playerData.Deltas.Mmr = CalculateMmrDelta(replayData.WinnerTeamData.ExpectedResult, playerImpact, mmrOptions.EloK);
        playerData.Deltas.Consistency = Math.Abs(teamData.ExpectedResult - teamData.ActualResult) < 0.50 ? 1 : 0;
        playerData.Deltas.Confidence = 1 - Math.Abs(teamData.ExpectedResult - teamData.ActualResult);
    }

    private static void SetLeaverPlayerDeltas(PlayerData playerData, ReplayData replayData, double playerImpact, MmrOptions mmrOptions)
    {
        playerData.Deltas.Mmr = -1 * CalculateMmrDelta(replayData.LoserTeamData.ExpectedResult, playerImpact, mmrOptions.EloK); //ToDo
        playerData.Deltas.Consistency = 0;
        playerData.Deltas.Confidence = 0;
    }

    private static double GetPlayerImpact(TeamData teamData, ReplayData replayData, CalcRating lastPlRating, MmrOptions mmrOptions)
    {
        double factor_playerToTeamMates = PlayerToTeamMates(teamData.Mmr, lastPlRating.Mmr, teamData.Players.Length);
        double factor_consistency = GetCorrectedRevConsistency(1 - lastPlRating.Consistency);
        double factor_confidence = GetCorrectedConfidenceFactor(lastPlRating.Confidence, replayData.Confidence);

        return 1
            * (mmrOptions.UseFactorToTeamMates ? factor_playerToTeamMates : 1.0)
            * (mmrOptions.UseConsistency ? factor_consistency : 1.0)
            * (mmrOptions.UseConfidence ? factor_confidence : 1.0);
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

    //# Commander-Mmr
    private static double GetCommandersComboMmr(ReplayData replayData, TeamData teamData, Dictionary<CmdrMmmrKey, CmdrMmmrValue> cmdrMmrDic)
    {
        if (replayData.IsInvalid)
        {
            return 1.0;
        }

        double commandersComboMMRSum = 0;

        for (int playerIndex = 0; playerIndex < teamData.Players.Length; playerIndex++)
        {
            var playerCmdr = teamData.Players[playerIndex].Race;

            double synergySum = 0;
            double antiSynergySum = 0;

            for (int synergyPlayerIndex = 0; synergyPlayerIndex < teamData.Players.Length; synergyPlayerIndex++)
            {
                if (playerIndex == synergyPlayerIndex)
                {
                    continue;
                }

                var synergyPlayerCmdr = teamData.Players[synergyPlayerIndex].Race;

                var synergy = cmdrMmrDic[new CmdrMmmrKey() { Race = playerCmdr, OppRace = synergyPlayerCmdr }];

                synergySum += ((1 / 2.0) * synergy.SynergyMmr);
            }

            for (int antiSynergyPlayerIndex = 0; antiSynergyPlayerIndex < teamData.Players.Length; antiSynergyPlayerIndex++)
            {
                var antiSynergyPlayerCmdr = teamData.Players[antiSynergyPlayerIndex].OppRace;

                var antiSynergy = cmdrMmrDic[new CmdrMmmrKey() { Race = playerCmdr, OppRace = antiSynergyPlayerCmdr }];

                if (playerIndex == antiSynergyPlayerIndex)
                {
                    antiSynergySum += (MmrOptions.ownMatchupPercentage * antiSynergy.AntiSynergyMmr);
                }
                else
                {
                    antiSynergySum += (MmrOptions.matesMatchupsPercentage * antiSynergy.AntiSynergyMmr);
                }
            }

            commandersComboMMRSum +=
                (MmrOptions.antiSynergyPercentage * antiSynergySum)
                + (MmrOptions.synergyPercentage * synergySum);
        }

        return commandersComboMMRSum / 3;
    }

    private static void SetCommandersComboMmr(ReplayData replayData, TeamData teamData, Dictionary<CmdrMmmrKey, CmdrMmmrValue> cmdrMmrDic)
    {
        if (replayData.IsInvalid) {
            return;
        }

        for (int playerIndex = 0; playerIndex < teamData.Players.Length; playerIndex++) {
            var playerCmdr = teamData.Players[playerIndex].Race;

            for (int synergyPlayerIndex = 0; synergyPlayerIndex < teamData.Players.Length; synergyPlayerIndex++) {
                if (playerIndex == synergyPlayerIndex) {
                    continue;
                }

                var synergyPlayerCmdr = teamData.Players[synergyPlayerIndex].Race;

                var synergy = cmdrMmrDic[new CmdrMmmrKey() { Race = playerCmdr, OppRace = synergyPlayerCmdr }];

                synergy.SynergyMmr += teamData.Players[playerIndex].Deltas.CommanderMmr / 2;
            }

            for (int antiSynergyPlayerIndex = 0; antiSynergyPlayerIndex < teamData.Players.Length; antiSynergyPlayerIndex++) {
                var antiSynergyPlayerCmdr = teamData.Players[antiSynergyPlayerIndex].OppRace;

                var antiSynergy = cmdrMmrDic[new CmdrMmmrKey() { Race = playerCmdr, OppRace = antiSynergyPlayerCmdr }];

                antiSynergy.AntiSynergyMmr += teamData.Players[playerIndex].Deltas.CommanderMmr;
            }
        }
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


    //# Formulas
    private static double GetCorrectedConfidenceFactor(double playerConfidence, double replayConfidence)
    {
        double totalConfidenceFactor = (0.5 * (1 - GetConfidenceFactor(playerConfidence))) + (0.5 * GetConfidenceFactor(replayConfidence));
        return 1 + MmrOptions.confidenceImpact * (totalConfidenceFactor - 1);
    }

    private static double GetConfidenceFactor(double confidence)
    {
        double variance = ((MmrOptions.distributionMult * 0.4) + (1 - confidence));

        return MmrOptions.distributionMult * (1 / (Math.Sqrt(2 * Math.PI) * Math.Abs(variance)));
    }

    private static double CalculateMmrDelta(double elo, double playerImpact, double eloK = 32)
    {
        return (double)(eloK * (1 - elo) * playerImpact);
    }

    private static double GetCorrectedRevConsistency(double raw_revConsistency)
    {
        return 1 + MmrOptions.consistencyImpact * (raw_revConsistency - 1);
    }

    private static double PlayerToTeamMates(double teamMmrMean, double playerMmr, int teamSize)
    {
        return teamSize * (playerMmr / (teamMmrMean * teamSize));
    }

    public static double EloExpectationToWin(double ratingOne, double ratingTwo, double clip = 400)
    {
        return 1.0 / (1.0 + Math.Pow(10.0, (2.0 / clip) * (ratingTwo - ratingOne)));
    }
}
