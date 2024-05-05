
using System.Text.RegularExpressions;

namespace dsstats.shared;

public record GroupState
{
    public RatingType RatingType { get; set; } = RatingType.StdTE;
    public RatingCalcType RatingCalcType { get; set; } = RatingCalcType.Dsstats;
    public Guid GroupId { get; set; }
    public int Visitors { get; set; }
    public HashSet<string> ReplayHashes { get; set; } = [];
    public List<PlayerState> PlayerStates { get; set; } = [];
    public IhMatch IhMatch { get; set; } = new();
}

public record PlayerState
{
    public PlayerId PlayerId { get; set; } = new();
    public string Name { get; set; } = string.Empty;
    public List<PlayerId> PlayedWith { get; set; } = [];
    public List<PlayerId> PlayedAgainst { get; set; } = [];
    public int Games { get; set; }
    public int Observer { get; set; }
    public bool InQueue {  get; set; }
    public bool Joined { get; set; }
    public int RatingStart { get; set; }
    public int CurrentRating { get; set; }
    public QueuePriority QueuePriority { get; set; } = QueuePriority.High;
}

public static class GroupStateExtensions
{
    public static void FillTeam(this GroupState groupState, int teamId)
    {
        var team = groupState.IhMatch.Teams[teamId];
        var remainingPlayers = team.Slots.Count(x => x.PlayerId.ToonId == 0);

        if (remainingPlayers <= 0)
        {
            return;
        }

        var players = groupState.PlayerStates.Where(x => x.InQueue).ToList();
        int avgRating = Convert.ToInt32(players.Average(a => a.RatingStart));

        var orderedPlayers = players
            .OrderByDescending(o => o.QueuePriority)
                .ThenByDescending(o => o.RatingStart)
            .ToList();
        var availablePlayers = new List<PlayerState>(orderedPlayers)
            .Where(x => !groupState.IhMatch.Teams.Any(a => a.Slots.Any(b => b.PlayerId == x.PlayerId)))
            .ToList();

        if (availablePlayers.Count < remainingPlayers)
        {
            return;
        }

        for (int i = 0; i < team.Slots.Length; i++)
        {
            var slot = team.Slots[i];
            if (slot.PlayerId.ToonId != 0)
            {
                continue;
            }
            
            var closestPlayer = SetClosestPlayer(groupState.IhMatch,
                                     teamId,
                                     i,
                                     avgRating,
                                     availablePlayers,
                                     groupState);

            availablePlayers.Remove(closestPlayer);
        }
    }

    public static void CreateMatch(this GroupState groupState)
    {
        var players = groupState.PlayerStates.Where(x => x.InQueue).ToList();
        if (players.Count < 6)
        {
            return;
        }

        int teamCount = 2;
        int avgRating = Convert.ToInt32(players.Average(a => a.RatingStart));
        var orderedPlayers = players
            .OrderByDescending(o => o.QueuePriority)
                .ThenByDescending(o => o.RatingStart)
            .ToList();
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
        groupState.IhMatch = match;
    }

    private static PlayerState SetClosestPlayer(IhMatch match,
                                         int teamId,
                                         int slotId,
                                         int avgRating,
                                         List<PlayerState> availablePlayers,
                                         GroupState groupState)
    {
        var team = match.Teams[teamId];
        var slot = team.Slots[slotId];
        var closestPlayers = availablePlayers
            .OrderByDescending(o => o.QueuePriority)
                .ThenBy(p => Math.Abs((team.Rating * slotId + p.RatingStart) / (slotId - avgRating)))
            .Take(3)
            .ToList();

        Dictionary<PlayerId, KeyValuePair<int, int>> playerScores = [];

        for (int i = 0; i < closestPlayers.Count; i++)
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
}