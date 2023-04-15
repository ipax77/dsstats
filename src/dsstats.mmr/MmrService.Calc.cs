using dsstats.mmr.Extensions;
using dsstats.mmr.ProcessData;
using pax.dsstats.shared;

using TeamData = dsstats.mmr.ProcessData.TeamData;

namespace dsstats.mmr;
using Maths;

public static partial class MmrService
{
    private static void CalculateRatingsDeltas(ReplayData replayData,
                                               TeamData teamData,
                                               MmrOptions mmrOptions)
    {
        foreach (var playerData in teamData.Players)
        {
            double playerImpact = 1;

            if (!playerData.IsLeaver)
            {
                playerImpact *= replayData.LeaverImpact;

                SetPlayerDeltas(playerData, teamData, playerImpact, mmrOptions);
            }
            else
            {
                SetLeaverPlayerDeltas(playerData, teamData, playerImpact, mmrOptions);
            }
        }
    }

    private static void SetPlayerDeltas(PlayerData playerData, TeamData teamData, double playerImpact, MmrOptions mmrOptions)
    {
        var ratingAfter = GaussianElo.GetRatingAfter(
            playerData.Distribution,
            teamData.ActualResult,
            teamData.ExpectedResult,
            teamData.Prediction,
            playerImpact,
            mmrOptions
            );

        playerData.Deltas.Mmr = ratingAfter.Mean - playerData.Mmr;
        playerData.Deltas.Deviation = ratingAfter.Deviation - playerData.Deviation;
    }

    private static void SetLeaverPlayerDeltas(PlayerData playerData, TeamData teamData, double playerImpact, MmrOptions mmrOptions)
    {
        var ratingAfter = GaussianElo.GetRatingAfter(
            playerData.Distribution,
            0,
            teamData.ExpectedResult,
            teamData.Prediction,
            playerImpact,
            mmrOptions
            );

        playerData.Deltas.Mmr = ratingAfter.Mean - playerData.Mmr;
        playerData.Deltas.Deviation = ratingAfter.Deviation - playerData.Deviation;
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
            double confidenceAfter = player.Deviation + player.Deltas.Deviation;

            var currentPlayerRating = mmrIdRatings[player.MmrId];
            ratings.Add(new()
            {
                GamePos = player.ReplayPlayer.GamePos,
                Rating = MathF.Round((float)mmrAfter, 2),
                RatingChange = MathF.Round((float)(mmrAfter - player.Mmr), 2),
                Games = currentPlayerRating.Games,
                Deviation = (float)player.Deviation,
                ReplayPlayerId = player.ReplayPlayer.ReplayPlayerId,
            });

            currentPlayerRating.Deviation = confidenceAfter;
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
