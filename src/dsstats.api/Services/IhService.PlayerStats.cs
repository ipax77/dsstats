using dsstats.db8;
using dsstats.shared;
using Microsoft.EntityFrameworkCore;

namespace dsstats.api.Services;

public partial class IhService
{
    private async Task UpdatePlayerStats(GroupStateV2 groupState)
    {
        if (!groupReplays.TryGetValue(groupState.GroupId, out var replays)
            || replays is null)
        {
            return;
        }

        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        groupState.PlayerStates.ForEach(f => f.RatingChange = 0);

        foreach (var replay in replays)
        {
            if (replay.ReplayRating is null)
            {
                replay.ReplayRating = await GetReplayRating(replay.Replay.ReplayHash, context);
            }

            foreach (var replayPlayer in replay.Replay.ReplayPlayers)
            {
                PlayerId playerId = new(replayPlayer.Player.ToonId, replayPlayer.Player.RealmId, replayPlayer.Player.RegionId);
                var playerState = groupState.PlayerStates.FirstOrDefault(f => f.PlayerId == playerId);
                if (playerState is null)
                {
                    RequestNames requestNames = new RequestNames(replayPlayer.Name, replayPlayer.Player.ToonId, 
                        replayPlayer.Player.RegionId, replayPlayer.Player.RealmId);

                    playerState = await AddPlayerToGroup(groupState.GroupId, requestNames, true);
                    if (playerState is null)
                    {
                        continue;
                    }
                }

                var rating = replay.ReplayRating?.RepPlayerRatings.FirstOrDefault(f => f.GamePos == replayPlayer.GamePos);
                playerState.RatingChange += Convert.ToInt32(rating?.RatingChange ?? 0);
            }
        }
    }

    private static async Task<ReplayRatingDto?> GetReplayRating(string replayHash, ReplayContext context)
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
