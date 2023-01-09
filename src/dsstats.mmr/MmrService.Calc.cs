using dsstats.mmr.Extensions;
using dsstats.mmr.ProcessData;
using pax.dsstats.shared;
using pax.dsstats.shared.Raven;

using pax.dsstats;
using TeamData = dsstats.mmr.ProcessData.TeamData;

namespace dsstats.mmr;

public static partial class MmrService
{
    private static void CalculateRatingsDeltas(Dictionary<int, CalcRating> mmrIdRatings,
                                               ReplayData replayData,
                                               TeamData teamData,
                                               MmrOptions mmrOptions)
    {
        int replayLeaverCount = replayData.WinnerTeamData.Players.Count(x => x.IsLeaver) + replayData.LoserTeamData.Players.Count(x => x.IsLeaver);
        int teamLeaverCount = teamData.Players.Count(x => x.IsLeaver);

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

            playerImpact *= LeaverHandlingFactor(replayData, playerData, replayLeaverCount, teamLeaverCount);

            playerData.Deltas.Mmr = CalculateMmrDelta(replayData.WinnerTeamData.ExpectedResult, playerImpact, mmrOptions.EloK);
            //playerData.Deltas.Consistency = MmrOptions.consistencyDeltaMult * 2 * (replayData.WinnerTeamData.ExpectedResult - 0.50);
            playerData.Deltas.Consistency = Math.Abs(teamData.ExpectedResult - teamData.ActualResult) < 0.50 ? 1 : 0;
            playerData.Deltas.Confidence = 0;//1 - Math.Abs(teamData.ExpectedResult - teamData.ActualResult);

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

    private static double LeaverHandlingFactor(ReplayData replayData, PlayerData playerData, int replayLeaverCount, int teamLeaverCount)
    {
        if (playerData.IsLeaver)
        {
            return 1;
        }
        if (teamLeaverCount == 3)
        {
            return 1;
        }
        if (replayLeaverCount == 0)
        {
            return 1;
        }

        if (replayLeaverCount == 1) // 1 Leaver only
        {
            return 0.5;
        }
        else if (replayLeaverCount == 2 * teamLeaverCount) // 1 Leaver per Team, 2 Leavers per Team
        {
            var duration1avg = replayData.WinnerTeamData.Players.Where(x => x.IsLeaver).Sum(x => x.Duration) / teamLeaverCount;
            var duration2avg = replayData.LoserTeamData.Players.Where(x => x.IsLeaver).Sum(x => x.Duration) / teamLeaverCount;

            if (Math.Abs(duration1avg - duration2avg) < replayData.Duration * 0.15) // > 15% time
            {
                return 1;
            }
            else
            {
                return 0.5;
            }
        }
        else // 2 Leavers and 0/1 Leaver
        {
            return 0.25;
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
}
