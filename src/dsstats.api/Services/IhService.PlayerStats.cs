using dsstats.db8;
using dsstats.shared;
using Microsoft.EntityFrameworkCore;

namespace dsstats.api.Services;

public partial class IhService
{
    private async Task UpdatePlayerStats(GroupState groupState)
    {
        if (!groupReplays.TryGetValue(groupState.GroupId, out var replays)
            || replays is null)
        {
            return;
        }

        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        List<PlayerStats> playerStats = [];

        foreach (var replay in replays)
        {
            if (replay.ReplayRating is null)
            {
                replay.ReplayRating = await GetReplayRating(replay.Replay.ReplayHash, context);
            }

            foreach (var replayPlayer in replay.Replay.ReplayPlayers)
            {
                PlayerId playerId = new(replayPlayer.Player.ToonId, replayPlayer.Player.RealmId, replayPlayer.Player.RegionId);
                var playerStat = playerStats.FirstOrDefault(f => f.PlayerId ==  playerId);
                if (playerStat is null)
                {
                    playerStat = new PlayerStats()
                    {
                        PlayerId = playerId,
                    };
                    playerStats.Add(playerStat);
                }

                var rating = replay.ReplayRating?.RepPlayerRatings.FirstOrDefault(f => f.GamePos == replayPlayer.GamePos);
                playerStat.RatingChange += rating?.RatingChange ?? 0;
                if (replayPlayer.PlayerResult == PlayerResult.Win)
                {
                    playerStat.Wins++;
                }
            }
        }
        groupState.PlayerStats = playerStats;
    }

    private async Task<ReplayRatingDto?> GetReplayRating(string replayHash, ReplayContext context)
    {
        return await context.ReplayRatings
            .Where(x => x.Replay.ReplayHash == replayHash)
            .Select(s => new ReplayRatingDto()
            {
                RatingType = s.RatingType,
                LeaverType = s.LeaverType,
                ExpectationToWin = s.ExpectationToWin,
                ReplayId = s.ReplayId,
                IsPreRating = s.IsPreRating,
                RepPlayerRatings = s.RepPlayerRatings.Select(t => new RepPlayerRatingDto()
                {
                    GamePos = t.GamePos,
                    Rating = (int)t.Rating,
                    RatingChange = t.RatingChange,
                    Games = t.Games,
                    Consistency = t.Consistency,
                    Confidence = t.Confidence,
                    ReplayPlayerId = t.ReplayPlayerId,
                }).ToList()
            })
            .FirstOrDefaultAsync();
    }
}
