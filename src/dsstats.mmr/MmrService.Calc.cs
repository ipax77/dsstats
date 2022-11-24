
using dsstats.mmr.Extensions;
using dsstats.mmr.ProcessData;
using pax.dsstats.shared;
using pax.dsstats.shared.Raven;
using TeamData = dsstats.mmr.ProcessData.TeamData;

namespace dsstats.mmr;

public static partial class MmrService
{
    //# Calculate
    private static double CalculateRatingsDeltas(Dictionary<int, CalcRating> mmrIdRatings,
                                               ReplayData replayData,
                                               TeamData teamData,
                                               MmrOptions mmrOptions,
                                               double maxMmr)
    {
        foreach (var playerData in teamData.Players)
        {
            var lastPlRating = mmrIdRatings[GetMmrId(playerData.ReplayPlayer.Player)];

            double playerConsistency = lastPlRating.Consistency;
            double playerConfidence = lastPlRating.Confidence;
            double playerMmr = lastPlRating.Mmr;

            //if (playerMmr > maxMmrCmdr)
            //{
            //    maxMmrCmdr = playerMmr;
            //}

            double factor_playerToTeamMates = PlayerToTeamMates(teamData.Mmr, playerMmr, teamData.Players.Length);
            double factor_consistency = GetCorrectedRevConsistency(1 - playerConsistency);
            double factor_confidence = GetCorrectedConfidenceFactor(playerConfidence, replayData.Confidence);

            double playerImpact = 1
                * (mmrOptions.UseFactorToTeamMates ? factor_playerToTeamMates : 1.0)
                * (mmrOptions.UseConsistency ? factor_consistency : 1.0)
                * (mmrOptions.UseConfidence ? factor_confidence : 1.0);

            playerData.Deltas.Mmr = CalculateMmrDelta(replayData.WinnerTeamData.ExpectedResult, playerImpact);
            playerData.Deltas.Consistency = consistencyDeltaMult * 2 * (replayData.WinnerTeamData.ExpectedResult - 0.50);
            playerData.Deltas.Confidence = 1 - Math.Abs(teamData.ExpectedResult - teamData.ActualResult);

            //double commandersMmrImpact = Math.Pow(startMmr, (playerMmr / maxMmrCmdr)) / startMmr;
            //playerData.CommanderMmrDelta = CalculateMmrDelta(replayData.WinnerCmdrExpectationToWin, 1, commandersMmrImpact);

            if (playerData.IsLeaver)
            {
                playerData.Deltas.Consistency *= -1;

                playerData.Deltas.Mmr *= -1;
                playerData.Deltas.CommanderMmr = 0;
            }
            else if (!teamData.IsWinner)
            {
                playerData.Deltas.Mmr *= -1;
                playerData.Deltas.CommanderMmr *= -1;
            }

            if (Math.Abs(playerData.Deltas.Mmr) > eloK * teamData.Players.Length)
            {
                // todo: no Exceptions prefered.
                throw new Exception("MmrDelta is bigger than eloK");
            }
        }
        return maxMmr;
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
            var currentPlayerRating = mmrIdRatings[GetMmrId(player.ReplayPlayer.Player)];

            int gamesCountBefore = currentPlayerRating.Games;
            double mmrBefore = currentPlayerRating.Mmr;
            double consistencyBefore = currentPlayerRating.Consistency;
            //double confidenceBeforeSummed = currentPlayerRating.Confidence * gamesCountBefore;
            double confidenceBefore = currentPlayerRating.Confidence;

            //int gamesCountAfter = gamesCountBefore + 1;
            double mmrAfter = mmrBefore + player.Deltas.Mmr;
            double consistencyAfter = consistencyBefore + player.Deltas.Consistency;
            //double confidenceAfter = (confidenceBeforeSummed + player.PlayerConfidenceDelta) / gamesCountAfter;
            double confidenceAfter = ((confidenceBefore * 0.50) + (player.Deltas.Confidence * 0.50));

            consistencyAfter = Math.Clamp(consistencyAfter, 0, 1);
            confidenceAfter = Math.Clamp(confidenceAfter, 0, 1);
            mmrAfter = Math.Max(1, mmrAfter);

            changes.Add(new PlChange() { Pos = player.ReplayPlayer.GamePos, Change = mmrAfter - mmrBefore });

            currentPlayerRating.Consistency = consistencyAfter;
            currentPlayerRating.Confidence = confidenceAfter;
            currentPlayerRating.Games++;

            if (player.ReplayPlayer.PlayerResult == PlayerResult.Win) {
                currentPlayerRating.Wins++;
            }
            if (player.ReplayPlayer.Kills == maxKills) {
                currentPlayerRating.Mvp++;
            }
            if (player.ReplayPlayer.IsUploader) {
                currentPlayerRating.IsUploader = true;
            }

            currentPlayerRating.SetCmdr(player.ReplayPlayer.Race);
            currentPlayerRating.SetMmr(mmrAfter, gameTime);
        }
        return changes;
    }

    //# Commander-Mmr
    private static double GetCommandersComboMmr(TeamData teamData, Dictionary<CmdrMmmrKey, CmdrMmmrValue> cmdrMmrDic)
    {
        if (teamData.HasStd)
        {
            return 1.0;
        }

        double commandersComboMMRSum = 0;

        for (int playerIndex = 0; playerIndex < teamData.Players.Length; playerIndex++)
        {
            var playerCmdr = teamData.Players[playerIndex].ReplayPlayer.Race;
            if ((int)playerCmdr <= 3) {
                continue;
            }

            double synergySum = 0;
            double antiSynergySum = 0;

            for (int synergyPlayerIndex = 0; synergyPlayerIndex < teamData.Players.Length; synergyPlayerIndex++)
            {
                if (playerIndex == synergyPlayerIndex)
                {
                    continue;
                }

                var synergyPlayerCmdr = teamData.Players[synergyPlayerIndex].ReplayPlayer.Race;

                var synergy = cmdrMmrDic[new CmdrMmmrKey() { Race = playerCmdr, OppRace = synergyPlayerCmdr }];

                synergySum += ((1 / 2.0) * synergy.SynergyMmr);
            }

            for (int antiSynergyPlayerIndex = 0; antiSynergyPlayerIndex < teamData.Players.Length; antiSynergyPlayerIndex++)
            {
                var antiSynergyPlayerCmdr = teamData.Players[antiSynergyPlayerIndex].ReplayPlayer.OppRace;

                var antiSynergy = cmdrMmrDic[new CmdrMmmrKey() { Race = playerCmdr, OppRace = antiSynergyPlayerCmdr }];

                if (playerIndex == antiSynergyPlayerIndex)
                {
                    antiSynergySum += (OwnMatchupPercentage * antiSynergy.AntiSynergyMmr);
                }
                else
                {
                    antiSynergySum += (MatesMatchupsPercentage * antiSynergy.AntiSynergyMmr);
                }
            }

            commandersComboMMRSum +=
                (AntiSynergyPercentage * antiSynergySum)
                + (SynergyPercentage * synergySum);
        }

        return commandersComboMMRSum / 3;
    }

    private static void SetCommandersComboMmr(TeamData teamData, Dictionary<CmdrMmmrKey, CmdrMmmrValue> cmdrMmrDic)
    {
        if (teamData.HasStd) {
            return;
        }

        for (int playerIndex = 0; playerIndex < teamData.Players.Length; playerIndex++) {
            var playerCmdr = teamData.Players[playerIndex].ReplayPlayer.Race;
            if ((int)playerCmdr <= 3) {
                continue;
            }

            for (int synergyPlayerIndex = 0; synergyPlayerIndex < teamData.Players.Length; synergyPlayerIndex++) {
                if (playerIndex == synergyPlayerIndex) {
                    continue;
                }

                var synergyPlayerCmdr = teamData.Players[synergyPlayerIndex].ReplayPlayer.Race;
                if ((int)synergyPlayerCmdr <= 3) {
                    continue;
                }

                var synergy = cmdrMmrDic[new CmdrMmmrKey() { Race = playerCmdr, OppRace = synergyPlayerCmdr }];

                synergy.SynergyMmr += teamData.Players[playerIndex].Deltas.CommanderMmr / 2;
            }

            for (int antiSynergyPlayerIndex = 0; antiSynergyPlayerIndex < teamData.Players.Length; antiSynergyPlayerIndex++) {
                var antiSynergyPlayerCmdr = teamData.Players[antiSynergyPlayerIndex].ReplayPlayer.OppRace;
                if ((int)antiSynergyPlayerCmdr <= 3) {
                    continue;
                }

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
        SetTeamData(mmrIdRatings, replayData.WinnerTeamData, cmdrDic, mmrOptions);
        SetTeamData(mmrIdRatings, replayData.LoserTeamData, cmdrDic, mmrOptions);
        SetExpectationsToWin(replayData, mmrOptions);

        replayData.Confidence = (replayData.WinnerTeamData.Confidence + replayData.LoserTeamData.Confidence) / 2;
    }

    private static void SetTeamData(Dictionary<int, CalcRating> mmrIdRatings,
                                    TeamData teamData,
                                    Dictionary<CmdrMmmrKey, CmdrMmmrValue> cmdrDic,
                                    MmrOptions mmrOptions)
    {
        foreach (var playerData in teamData.Players)
        {
            SetPlayerData(mmrIdRatings, playerData);
        }

        teamData.Confidence = teamData.Players.Sum(p => p.Confidence) / teamData.Players.Length;
        teamData.Mmr = teamData.Players.Sum(p => p.Mmr) / teamData.Players.Length;

        if (mmrOptions.UseCommanderMmr)
        {
            teamData.CmdrComboMmr = GetCommandersComboMmr(teamData, cmdrDic);
        }
    }

    private static void SetExpectationsToWin(ReplayData replayData, MmrOptions mmrOptions)
    {
        double winnerPlayersExpectationToWin = EloExpectationToWin(replayData.WinnerTeamData.Mmr, replayData.LoserTeamData.Mmr);
        replayData.WinnerTeamData.ExpectedResult = winnerPlayersExpectationToWin;

        if (mmrOptions.UseCommanderMmr) {
            double winnerCmdrExpectationToWin = EloExpectationToWin(replayData.WinnerTeamData.CmdrComboMmr, replayData.LoserTeamData.CmdrComboMmr);
            replayData.WinnerTeamData.ExpectedResult = (winnerPlayersExpectationToWin + winnerCmdrExpectationToWin) / 2;
        }

        replayData.LoserTeamData.ExpectedResult = (1 - replayData.WinnerTeamData.ExpectedResult);
    }

    private static void SetPlayerData(Dictionary<int, CalcRating> mmrIdRatings, PlayerData playerData)
    {
        if (!mmrIdRatings.TryGetValue(GetMmrId(playerData.ReplayPlayer.Player), out var plRating))
        {
            plRating = mmrIdRatings[GetMmrId(playerData.ReplayPlayer.Player)] = new CalcRating()
            {
                Mmr = startMmr,
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
        double totalConfidenceFactor = (1 - GetConfidenceFactor(playerConfidence)) * GetConfidenceFactor(replayConfidence);
        return 1 + confidenceImpact * (totalConfidenceFactor - 1);
    }

    private static double GetConfidenceFactor(double confidence)
    {
        double variance = ((distributionMult * 0.4) + (1 - confidence));

        return distributionMult * (1 / (Math.Sqrt(2 * Math.PI) * Math.Abs(variance)));
    }

    private static double CalculateMmrDelta(double elo, double playerImpact)
    {
        return (double)(eloK * (1 - elo) * playerImpact);
    }

    private static double GetCorrectedRevConsistency(double raw_revConsistency)
    {
        return 1 + consistencyImpact * (raw_revConsistency - 1);
    }

    private static double PlayerToTeamMates(double teamMmrMean, double playerMmr, int teamSize)
    {
        return teamSize * (playerMmr / (teamMmrMean * teamSize));
    }

    private static double EloExpectationToWin(double ratingOne, double ratingTwo)
    {
        return 1.0 / (1.0 + Math.Pow(10.0, (2.0 / clip) * (ratingTwo - ratingOne)));
    }
}