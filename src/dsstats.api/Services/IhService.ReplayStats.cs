using dsstats.db8;
using dsstats.shared;
using Microsoft.EntityFrameworkCore;

namespace dsstats.api.Services;

public partial class IhService
{
    private async Task SetReplayStats(GroupState groupState, List<IhReplay> replays)
    {
        foreach (var replay in replays)
        {
            await SetReplayStat(groupState, replay);
        }
    }

    private async Task SetReplayStat(GroupState groupState, IhReplay replay)
    {
        foreach (var player in replay.Metadata.Players)
        {
            var groupPlayer = groupState.PlayerStates.FirstOrDefault(f => f.PlayerId == player.PlayerId);

            if (groupPlayer is null)
            {
                groupPlayer = new PlayerState()
                {
                    PlayerId = player.PlayerId,
                    Name = player.Name,
                    RatingStart = await GetRating(groupState, player.PlayerId)
                };
                groupState.PlayerStates.Add(groupPlayer);
            }

            if (player.Observer)
            {
                groupPlayer.Observer++;
            }
            else
            {
                groupPlayer.Games++;
            }
        }

        foreach (var player in replay.Replay.ReplayPlayers)
        {
            PlayerId playerId = new(player.Player.ToonId, player.Player.RealmId, player.Player.RegionId);
            var groupPlayer = groupState.PlayerStates.FirstOrDefault(f => f.PlayerId == playerId);

            if (groupPlayer is null)
            {
                groupPlayer = new PlayerState()
                {
                    PlayerId = playerId
                };
                groupState.PlayerStates.Add(groupPlayer);
            }
            groupPlayer.Name = player.Name;
            groupPlayer.Joined = false;

            foreach (var otherPlayer in replay.Replay.ReplayPlayers)
            {
                if (otherPlayer == player)
                {
                    continue;
                }

                PlayerId otherPlayerId = new(otherPlayer.Player.ToonId, otherPlayer.Player.RealmId, otherPlayer.Player.RegionId);
                if (player.Team == otherPlayer.Team)
                {
                    groupPlayer.PlayedWith.Add(otherPlayerId);
                }
                else
                {
                    groupPlayer.PlayedAgainst.Add(otherPlayerId);
                }
            }
        }
    }

    private async Task<int> GetRating(GroupState groupState, PlayerId playerId)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        double rating = 0;
        if (groupState.RatingCalcType == RatingCalcType.Dsstats)
        {
            rating = await context.PlayerRatings
                .Where(x => x.Player.ToonId == playerId.ToonId
                    && x.Player.RealmId == playerId.RealmId
                    && x.Player.RegionId == playerId.RealmId
                    && x.RatingType == groupState.RatingType)
                .Select(s => s.Rating)
                .FirstOrDefaultAsync();
        }
        else
        {
            rating = await context.ComboPlayerRatings
                .Where(x => x.Player.ToonId == playerId.ToonId
                    && x.Player.RealmId == playerId.RealmId
                    && x.Player.RegionId == playerId.RealmId
                    && x.RatingType == groupState.RatingType)
                .Select(s => s.Rating)
                .FirstOrDefaultAsync();
        }
        return Convert.ToInt32(rating);
    }
}
