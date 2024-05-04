
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
}

public static class GroupStateExtensions
{
    public static void CreateMatch(this GroupState groupState)
    {
        var players = groupState.PlayerStates.Where(x => x.InQueue).ToList();
        if (players.Count < 6)
        {
            return;
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
}