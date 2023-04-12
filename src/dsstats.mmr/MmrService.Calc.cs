using dsstats.mmr.Extensions;
using dsstats.mmr.ProcessData;
using pax.dsstats.shared;

using TeamData = dsstats.mmr.ProcessData.TeamData;

namespace dsstats.mmr;

public static partial class MmrService
{
    const double consistencyBeforePercentage = 0.99;
    const double confidenceBeforePercentage = 0.99;


    private static void CalculateRatingsDeltas(ReplayData replayData,
                                               TeamData teamData,
                                               MmrOptions mmrOptions)
    {
        foreach (var playerData in teamData.Players)
        {
            var playerImpact = GetPlayerImpact(playerData, teamData, replayData, mmrOptions);

            if (!playerData.IsLeaver)
            {
                playerImpact *= replayData.LeaverImpact;

                SetPlayerDeltas(playerData, teamData, replayData, playerImpact, mmrOptions);

                if (replayData.ReplayDsRDto.Maxleaver < 90 && mmrOptions.UseCommanderMmr)
                {
                    var commandersMmrImpact = (playerData.Mmr / mmrOptions.StartMmr) * playerData.Confidence;
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
                SetLeaverPlayerDeltas(playerData, teamData, replayData, playerImpact, mmrOptions);
            }
        }
    }

    private static void SetPlayerDeltas(PlayerData playerData, TeamData teamData, ReplayData replayData, double playerImpact, MmrOptions mmrOptions)
    {
        playerData.Deltas.Mmr = CalculateMmrDelta(replayData.WinnerTeamData.ExpectedResult, playerImpact, mmrOptions.EloK);
        playerData.Deltas.Consistency = Math.Abs(teamData.ExpectedResult - teamData.ActualResult) < 0.50 ? 1 : 0;
        playerData.Deltas.Confidence = 1 - Math.Abs(teamData.ExpectedResult - teamData.ActualResult);
    }

    private static void SetLeaverPlayerDeltas(PlayerData playerData, TeamData teamData, ReplayData replayData, double playerImpact, MmrOptions mmrOptions)
    {
        if (teamData.ActualResult == replayData.WinnerTeamData.ActualResult)
        {
            playerData.Deltas.Mmr = -1 * CalculateMmrDelta(replayData.LoserTeamData.ExpectedResult, playerImpact, mmrOptions.EloK); //ToDo
        }
        else
        {
            playerData.Deltas.Mmr = -1 * CalculateMmrDelta(replayData.WinnerTeamData.ExpectedResult, playerImpact, mmrOptions.EloK); //ToDo
        }

        playerData.Deltas.Consistency = 0;
        playerData.Deltas.Confidence = 0;
    }

    private static double GetPlayerImpact(PlayerData playerData, TeamData teamData, ReplayData replayData, MmrOptions mmrOptions)
    {
        double factor_playerToTeamMates = PlayerToTeamMates(teamData.Mmr, playerData.Mmr, teamData.Players.Length);
        double factor_consistency = GetCorrectedRevConsistency(1 - playerData.Consistency);
        double factor_confidence = GetCorrectedConfidenceFactor(playerData.Confidence, replayData.Confidence);

        return 1
            * (mmrOptions.UseFactorToTeamMates ? factor_playerToTeamMates : 1.0)
            * (mmrOptions.UseConsistency ? factor_consistency : 1.0)
            * (mmrOptions.UseConfidence ? factor_confidence : 1.0);
    }

    }

    private static List<RepPlayerRatingDto> AddPlayersRankings(Dictionary<int, CalcRating> mmrIdRatings,
                                                                  TeamData teamData,
                                                                  DateTime gameTime,
                                                                  int maxKills)
    {
        List<RepPlayerRatingDto> ratings = new();
        foreach (var player in teamData.Players)
        {
            double mmrAfter = player.Mmr + player.Deltas.Mmr;
            double consistencyAfter = ((player.Consistency * consistencyBeforePercentage) + (player.Deltas.Consistency * (1 - consistencyBeforePercentage)));
            double confidenceAfter = ((player.Confidence * confidenceBeforePercentage) + (player.Deltas.Confidence * (1 - confidenceBeforePercentage)));

            consistencyAfter = Math.Clamp(consistencyAfter, 0, 1);
            confidenceAfter = Math.Clamp(confidenceAfter, 0, 1);

            var currentPlayerRating = mmrIdRatings[player.MmrId];

            ratings.Add(new()
            {
                GamePos = player.ReplayPlayer.GamePos,
                Rating = MathF.Round((float)mmrAfter, 2),
                RatingChange = MathF.Round((float)(mmrAfter - player.Mmr), 2),
                Games = currentPlayerRating.Games,
                Consistency = MathF.Round((float)player.Consistency, 2),
                Confidence = MathF.Round((float)player.Confidence, 2),
                ReplayPlayerId = player.ReplayPlayer.ReplayPlayerId,
            });

            currentPlayerRating.Consistency = consistencyAfter;
            currentPlayerRating.Confidence = confidenceAfter;
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
            currentPlayerRating.SetMmr(mmrAfter, gameTime);
        }
        return ratings;
    }
}
