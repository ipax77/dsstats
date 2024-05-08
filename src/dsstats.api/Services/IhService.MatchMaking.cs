using dsstats.shared;

namespace dsstats.api.Services;

public partial class IhService
{
    private IhMatch? CreateMatch(GroupState groupState)
    {
        var players = groupState.PlayerStates.Where(x => x.InQueue).ToList();
        if (players.Count < 6)
        {
            return null;
        }

        int teamCount = 2;
        int avgRating = Convert.ToInt32(players.Average(a => a.RatingStart));
        var orderedPlayers = players.OrderByDescending(o => o.RatingStart).ToList();
        var availablePlayers = new List<PlayerState>(orderedPlayers);

        IhMatch match = new();

        for (int i = 0; i < teamCount; i++)
        {
            var player = orderedPlayers[i];
            availablePlayers.Remove(player);
            var slot = match.Teams[i].Slots[0];
            slot.PlayerId = player.PlayerId;
            slot.Name = player.Name;
            slot.Rating = player.RatingStart;
        }

        match.SetScores(groupState);

        for (int i = teamCount - 1; i >= 0; i--)
        {
            var team = match.Teams[i];
            int remainingPlayers = 2;

            while (remainingPlayers > 0 && availablePlayers.Count > 0)
            {
                var closestPlayer = SetClosestPlayer(match,
                                                     i,
                                                     remainingPlayers,
                                                     avgRating,
                                                     availablePlayers,
                                                     groupState);

                availablePlayers.Remove(closestPlayer);
                remainingPlayers--;
            }
        }
        return match;
    }

    private PlayerState SetClosestPlayer(IhMatch match,
                                         int teamId,
                                         int slotId,
                                         int avgRating,
                                         List<PlayerState> availablePlayers,
                                         GroupState groupState)
    {
        var team = match.Teams[teamId];
        var slot = team.Slots[slotId];
        var closestPlayers = availablePlayers
            .OrderBy(p => Math.Abs((team.Rating * 3 + p.RatingStart) / (3 - avgRating)))
            .ToList();

        Dictionary<PlayerId, KeyValuePair<int, int>> playerScores = [];

        for (int i = 0; i < Math.Min(3, closestPlayers.Count); i++)
        {
            var closestPlayer = closestPlayers[i];
            slot.PlayerId = closestPlayer.PlayerId;
            slot.Name = closestPlayer.Name;
            slot.Rating = closestPlayer.RatingStart;
            match.SetScores(groupState);
            playerScores[closestPlayer.PlayerId] = new(match.AgainstScore, team.WithScore);
        }

        var closestScorePlayerId = playerScores.OrderBy(o => o.Value.Key + o.Value.Value).First().Key;
        var closestScorePlayer = availablePlayers.First(f => f.PlayerId == closestScorePlayerId);
        slot.PlayerId = closestScorePlayer.PlayerId;
        slot.Name = closestScorePlayer.Name;
        slot.Rating = closestScorePlayer.RatingStart;
        match.SetScores(groupState);
        return closestScorePlayer;
    }

    public IhMatch GetIhMatch(IhReplay replay, GroupState groupState)
    {
        IhMatch match = new();

        foreach (var rp in replay.Replay.ReplayPlayers)
        {
            PlayerId playerId = new(rp.Player.ToonId, rp.Player.RealmId, rp.Player.RegionId);
            var pos = rp.GamePos > 3 ? rp.GamePos - 4 : rp.GamePos - 1;

            var player = groupState.PlayerStates.FirstOrDefault(f => f.PlayerId == playerId);
            if (player is null)
            {
                continue;
            }
            var team = rp.Team == 1 ? match.Teams[0] : match.Teams[1];
            var slot = team.Slots[pos];

            slot.PlayerId = playerId;
            slot.Rating = player.RatingStart;
            slot.Name = player.Name;
        }

        match.SetScores(groupState);

        return match;
    }
}

