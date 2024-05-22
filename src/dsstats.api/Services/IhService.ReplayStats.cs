using dsstats.db8;
using dsstats.shared;
using Microsoft.EntityFrameworkCore;

namespace dsstats.api.Services;

public partial class IhService
{
    private async Task SetReplayStats(GroupStateV2 groupState, List<IhReplay> replays)
    {
        foreach (var replay in replays)
        {
            await SetReplayStat(groupState, replay);
        }
        await UpdatePlayerStats(groupState);
    }

    private async Task SetReplayStat(GroupStateV2 groupState, IhReplay replay)
    {
        foreach (var player in replay.Metadata.Players)
        {
            if (player.PlayerId.ToonId == 0)
            {
                continue;
            }
            var groupPlayer = groupState.PlayerStates.FirstOrDefault(f => f.PlayerId == player.PlayerId);

            if (groupPlayer is null)
            {
                RequestNames requestNames = new(player.Name, player.PlayerId.ToonId, player.PlayerId.RegionId, player.PlayerId.RealmId);
                groupPlayer = await AddPlayerToGroup(groupState.GroupId, requestNames, true);
                if (groupPlayer is null)
                {
                    continue;
                }
            }
            if (player.Observer)
            {
                groupPlayer.Observer++;
                groupPlayer.ObsLastGame = true;
                groupPlayer.PlayedLastGame = false;
                groupPlayer.NewPlayer = false;
                groupPlayer.QueuePriority = QueuePriority.Medium;
            }
        }

        foreach (var player in replay.Replay.ReplayPlayers)
        {
            PlayerId playerId = new(player.Player.ToonId, player.Player.RealmId, player.Player.RegionId);
            var groupPlayer = groupState.PlayerStates.FirstOrDefault(f => f.PlayerId == playerId);

            if (groupPlayer is null)
            {
                RequestNames requestNames = new(player.Name, playerId.ToonId, playerId.RegionId, playerId.RealmId);
                groupPlayer = await AddPlayerToGroup(groupState.GroupId, requestNames, true);
                if (groupPlayer is null)
                {
                    continue;
                }
            }
            groupPlayer.PlayedLastGame = true;
            groupPlayer.ObsLastGame = false;
            groupPlayer.NewPlayer = false;
            groupPlayer.Name = player.Name;
            groupPlayer.QueuePriority = QueuePriority.Low;
            groupPlayer.Games++;
            if (player.PlayerResult == PlayerResult.Win)
            {
                groupPlayer.Wins++;
            }

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

    private async Task<(string, int)> GetNameAndRating(GroupStateV2 groupState, PlayerId playerId)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        if (groupState.RatingCalcType == RatingCalcType.Dsstats)
        {
            var namerating = await context.PlayerRatings
                .Where(x => x.Player.ToonId == playerId.ToonId
                    && x.Player.RealmId == playerId.RealmId
                    && x.Player.RegionId == playerId.RegionId
                    && x.RatingType == groupState.RatingType)
                .Select(s => new { s.Player.Name, s.Rating })
                .FirstOrDefaultAsync();
            return (namerating?.Name ?? "Unknown", Convert.ToInt32(namerating?.Rating ?? 1000));
        }
        else
        {
            var namerating = await context.ComboPlayerRatings
                .Where(x => x.Player.ToonId == playerId.ToonId
                    && x.Player.RealmId == playerId.RealmId
                    && x.Player.RegionId == playerId.RegionId
                    && x.RatingType == groupState.RatingType)
                .Select(s => new { s.Player.Name, s.Rating })
                .FirstOrDefaultAsync();
            return (namerating?.Name ?? "Unknown", Convert.ToInt32(namerating?.Rating ?? 1000));
        }
    }
}
