
using dsstats.mmr.Extensions;
using pax.dsstats.shared;
using pax.dsstats.shared.Raven;

namespace dsstats.mmr;

public static partial class MmrService
{
    private static void SetCommandersComboMmr(TeamData teamData, Dictionary<CmdrMmmrKey, CmdrMmmrValue> cmdrMmrDic)
    {
        if (teamData.Std)
        {
            return;
        }

        for (int playerIndex = 0; playerIndex < teamData.Players.Length; playerIndex++)
        {
            var playerCmdr = teamData.Players[playerIndex].ReplayPlayer.Race;

            for (int synergyPlayerIndex = 0; synergyPlayerIndex < teamData.Players.Length; synergyPlayerIndex++)
            {
                if (playerIndex == synergyPlayerIndex)
                {
                    continue;
                }

                var synergyPlayerCmdr = teamData.Players[synergyPlayerIndex].ReplayPlayer.Race;
                var synergy = cmdrMmrDic[new CmdrMmmrKey() { Race = playerCmdr, OppRace = synergyPlayerCmdr }];

                synergy.SynergyMmr += teamData.Players[playerIndex].CommanderMmrDelta / 2;
            }

            for (int antiSynergyPlayerIndex = 0; antiSynergyPlayerIndex < teamData.Players.Length; antiSynergyPlayerIndex++)
            {
                var antiSynergyPlayerCmdr = teamData.Players[antiSynergyPlayerIndex].ReplayPlayer.OppRace;

                var antiSynergy = cmdrMmrDic[new CmdrMmmrKey() { Race = playerCmdr, OppRace = antiSynergyPlayerCmdr }];

                antiSynergy.AntiSynergyMmr += teamData.Players[playerIndex].CommanderMmrDelta;
            }
        }
    }

    private static List<PlChange> AddPlayersRankings(Dictionary<int, CalcRating> mmrIdRatings,
                                                                  TeamData teamData,
                                                                  DateTime gameTime,
                                                                  int maxKills)
    {
        List<PlChange> changes = new();
        foreach (var player in teamData.Players)
        {
            var currentPlayerRating = mmrIdRatings[GetMmrId(player.ReplayPlayer.Player)];

            double mmrBefore = currentPlayerRating.Mmr;
            double consistencyBefore = currentPlayerRating.Consistency;
            double uncertaintyBeforeSummed = currentPlayerRating.Uncertainty * currentPlayerRating.Games;

            double mmrAfter = mmrBefore + player.PlayerMmrDelta;
            double consistencyAfter = consistencyBefore + player.PlayerConsistencyDelta;
            double uncertaintyAfter = (uncertaintyBeforeSummed + teamData.UncertaintyDelta) / (currentPlayerRating.Games + 1);

            consistencyAfter = Math.Clamp(consistencyAfter, 0, 1);
            mmrAfter = Math.Max(1, mmrAfter);

            changes.Add(new PlChange() { Pos = player.ReplayPlayer.GamePos, Change = mmrAfter - mmrBefore });

            currentPlayerRating.Consistency = (float)consistencyAfter;
            currentPlayerRating.Uncertainty = (float)uncertaintyAfter;
            currentPlayerRating.Games++;
            if (player.ReplayPlayer.PlayerResult == PlayerResult.Win)
            {
                currentPlayerRating.Wins++;
            }
            if (player.ReplayPlayer.Kills == maxKills)
            {
                currentPlayerRating.Mvp++;
            }
            if (player.ReplayPlayer.IsUploader)
            {
                currentPlayerRating.IsUploader = true;
            }
            currentPlayerRating.SetCmdr(player.ReplayPlayer.Race);
            currentPlayerRating.SetMmr((float)mmrAfter, gameTime);
        }
        return changes;
    }

    private static void FixMmrEquality(TeamData teamData, TeamData oppTeamData)
    {
        PlayerData[] posDeltaPlayers =
            teamData.Players.Where(x => x.PlayerMmrDelta >= 0).Concat(
            oppTeamData.Players.Where(x => x.PlayerMmrDelta >= 0)).ToArray();

        PlayerData[] negPlayerDeltas =
            teamData.Players.Where(x => x.PlayerMmrDelta < 0).Concat(
            oppTeamData.Players.Where(x => x.PlayerMmrDelta < 0)).ToArray();



        double absSumPosDeltas = Math.Abs(teamData.Players.Sum(x => Math.Max(0, x.PlayerMmrDelta)) + oppTeamData.Players.Sum(x => Math.Max(0, x.PlayerMmrDelta)));
        double absSumNegDeltas = Math.Abs(teamData.Players.Sum(x => Math.Min(0, x.PlayerMmrDelta)) + oppTeamData.Players.Sum(x => Math.Min(0, x.PlayerMmrDelta)));
        double absSumAllDeltas = absSumPosDeltas + absSumNegDeltas;

        if (absSumPosDeltas == 0 || absSumNegDeltas == 0)
        {
            foreach (var player in teamData.Players)
            {
                player.PlayerMmrDelta = 0;
            }
            foreach (var player in oppTeamData.Players)
            {
                player.PlayerMmrDelta = 0;
            }
            return;
        }

        foreach (var posDeltaPlayer in posDeltaPlayers)
        {
            posDeltaPlayer.PlayerMmrDelta *= (absSumAllDeltas / (absSumPosDeltas * 2));
        }
        foreach (var negDeltaPlayer in negPlayerDeltas)
        {
            negDeltaPlayer.PlayerMmrDelta *= (absSumAllDeltas / (absSumNegDeltas * 2));
        }
    }

    private static double CalculateRatingsDeltas(Dictionary<int, CalcRating> mmrIdRatings,
                                               ReplayProcessData replayProcessData,
                                               TeamData teamData,
                                               MmrOptions mmrOptions,
                                               double maxMmr)
    {
        foreach (var player in teamData.Players)
        {
            var lastPlRating = mmrIdRatings[GetMmrId(player.ReplayPlayer.Player)];
            double playerConsistency = lastPlRating.Consistency;

            double playerMmr = lastPlRating.Mmr;
            if (playerMmr > maxMmr)
            {
                maxMmr = playerMmr;
            }

            double factor_playerToTeamMates = PlayerToTeamMates(teamData.PlayersMeanMmr, playerMmr, teamData.Players.Length);
            double factor_consistency = GetCorrectedRevConsistency(1 - playerConsistency);
            double factor_uncertainty = 1 - replayProcessData.Uncertainty;

            double playerImpact = 1
                * (mmrOptions.UseFactorToTeamMates ? factor_playerToTeamMates : 1.0)
                * (mmrOptions.UseConsistency ? factor_consistency : 1.0)
                * (mmrOptions.UseUncertanity ? factor_uncertainty : 1.0);

            var mcv = mmrOptions.UseCommanderMmr ? (1 - replayProcessData.WinnerCmdrExpectationToWin) : 1;

            player.PlayerMmrDelta = CalculateMmrDelta(replayProcessData.WinnerPlayersExpectationToWin, playerImpact, mcv);
            player.PlayerConsistencyDelta = consistencyDeltaMult * 2 * (replayProcessData.WinnerPlayersExpectationToWin - 0.50);


            double commandersMmrImpact = Math.Pow(startMmr, (playerMmr / maxMmr)) / startMmr;
            player.CommanderMmrDelta = CalculateMmrDelta(replayProcessData.WinnerCmdrExpectationToWin, 1, commandersMmrImpact);

        }
        return maxMmr;
    }

    private static double CalculateMmrDelta(double elo, double playerImpact, double mcv)
    {
        return (double)(eloK * mcv * (1 - elo) * playerImpact);
    }

    private static double GetCorrectedRevConsistency(double raw_revConsistency)
    {
        return 1 + consistencyImpact * (raw_revConsistency - 1);
    }

    private static double PlayerToTeamMates(double teamMmrMean, double playerMmr, int teamSize)
    {
        return teamSize * (playerMmr / (teamMmrMean * teamSize));
    }

    private static void SetExpectationsToWin(Dictionary<int, CalcRating> mmrIdRatings, ReplayProcessData replayProcessData)
    {
        replayProcessData.WinnerPlayersExpectationToWin = EloExpectationToWin(replayProcessData.WinnerTeamData.PlayersMeanMmr, replayProcessData.LoserTeamData.PlayersMeanMmr);
        replayProcessData.WinnerCmdrExpectationToWin = EloExpectationToWin(replayProcessData.WinnerTeamData.CmdrComboMmr, replayProcessData.LoserTeamData.CmdrComboMmr);

        double winnerTotalExpectationToWin = replayProcessData.WinnerPlayersExpectationToWin/* * replayProcessData.WinnerCmdrExpectationToWin*/;
        replayProcessData.WinnerTeamData.UncertaintyDelta = 1 - winnerTotalExpectationToWin;
        replayProcessData.LoserTeamData.UncertaintyDelta = 1 - winnerTotalExpectationToWin;

        double uncertaintyWinnerTeam = replayProcessData.WinnerTeamData.Players.Sum(x => mmrIdRatings[GetMmrId(x.ReplayPlayer.Player)].Uncertainty)
            / replayProcessData.WinnerTeamData.Players.Length;
        double uncertaintyLoserTeam = replayProcessData.LoserTeamData.Players.Sum(x => mmrIdRatings[GetMmrId(x.ReplayPlayer.Player)].Uncertainty)
            / replayProcessData.LoserTeamData.Players.Length;
        replayProcessData.Uncertainty = (uncertaintyWinnerTeam + uncertaintyLoserTeam) / 2;
    }

    private static double EloExpectationToWin(double ratingOne, double ratingTwo)
    {
        return 1.0 / (1.0 + Math.Pow(10.0, (2.0 / clip) * (ratingTwo - ratingOne)));
    }

    private static void SetMmrs(Dictionary<int, CalcRating> mmrIdRatings, Dictionary<CmdrMmmrKey, CmdrMmmrValue> cmdrMmrDic, TeamData teamData, DateTime gametime)
    {
        teamData.CmdrComboMmr = GetCommandersComboMmr(teamData, cmdrMmrDic);
        teamData.PlayersMeanMmr = GetOrCreatePlayersComboMmr(mmrIdRatings, teamData, gametime);
    }

    private static double GetCommandersComboMmr(TeamData teamData, Dictionary<CmdrMmmrKey, CmdrMmmrValue> cmdrMmrDic)
    {
        if (teamData.Std)
        {
            return 1.0;
        }

        double commandersComboMMRSum = 0;

        for (int playerIndex = 0; playerIndex < teamData.Players.Length; playerIndex++)
        {
            var playerCmdr = teamData.Players[playerIndex].ReplayPlayer.Race;

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

    private static double GetOrCreatePlayersComboMmr(Dictionary<int, CalcRating> mmrIdRatings, TeamData teamData, DateTime gametime)
    {
        double teamMmr = 0;

        foreach (var playerData in teamData.Players)
        {
            if (!mmrIdRatings.ContainsKey(GetMmrId(playerData.ReplayPlayer.Player)))
            {
                mmrIdRatings[GetMmrId(playerData.ReplayPlayer.Player)] = new()
                {
                    Mmr = startMmr,
                    Consistency = 0,
                    Uncertainty = 0.5f,
                    MmrOverTime = new()
                    {
                        new() { Date = gametime.ToString(@"yyyyMMdd"), Mmr = startMmr }
                    }
                };
                teamMmr += startMmr;
            }
            else
            {
                teamMmr += mmrIdRatings[GetMmrId(playerData.ReplayPlayer.Player)].Mmr;
            }
        }
        return teamMmr / teamData.Players.Length;
    }
}