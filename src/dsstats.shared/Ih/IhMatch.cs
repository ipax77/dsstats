
namespace dsstats.shared;

public record IhMatch
{
    public IhTeam[] Teams { get; set; } = [new(), new()];
    public int AgainstScore { get; set; }
    public int RatingGap { get; set; }
}

public record IhTeam
{
    public IhSlot[] Slots { get; set; } = [new(), new(), new()];
    public int WithScore { get; set; }
    public int Rating { get; set; }
}

public record IhSlot
{
    public PlayerId PlayerId { get; set; } = new();
    public string Name { get; set; } = "Empty";
    public int Rating { get; set; }
}

public static class IhMatchExtensions
{
    public static void SetScores(this IhMatch match, GroupStateV2 groupState)
    {
        foreach (var team in match.Teams)
        {
            SetWithScore(team, groupState);
        }
        SetAgainstScore(match, groupState);
        SetRatingGap(match);
    }

    private static void SetRatingGap(IhMatch match)
    {
        if (match.Teams.Length != 2)
        {
            return;
        }

        foreach (var team in match.Teams)
        {
            team.Rating = team.Slots.Sum(s => s.Rating);
        }
        match.RatingGap = Math.Abs(match.Teams[0].Rating - match.Teams[1].Rating);
    }

    private static void SetWithScore(IhTeam team, GroupStateV2 groupState)
    {
        int withScore = 0;

        foreach (var slot in team.Slots)
        {
            var player = groupState.PlayerStates.FirstOrDefault(f => f.PlayerId == slot.PlayerId);
            if (player is null)
            {
                continue;
            }

            foreach (var otherSlot in team.Slots)
            {
                if (slot == otherSlot)
                {
                    continue;
                }
                withScore += player.PlayedWith.Count(x => x == otherSlot.PlayerId);
            }
        }
        team.WithScore = withScore;
    }

    private static void SetAgainstScore(IhMatch match, GroupStateV2 groupState)
    {
        if (match.Teams.Length != 2)
        {
            return;
        }

        int againstScore = 0;

        for (int i = 0; i < match.Teams.Length; i++)
        {
            var team1 = match.Teams[i];
            var team2 = i == 0 ? match.Teams[i + 1] : match.Teams[i - 1];

            foreach (var slot in team1.Slots)
            {
                var player = groupState.PlayerStates.FirstOrDefault(f => f.PlayerId == slot.PlayerId);
                if (player is null)
                {
                    continue;
                }
                foreach (var otherSlot in team2.Slots)
                {
                    againstScore += player.PlayedAgainst.Count(x => x == slot.PlayerId);
                }
            }
        }
        match.AgainstScore = againstScore;
    }
}