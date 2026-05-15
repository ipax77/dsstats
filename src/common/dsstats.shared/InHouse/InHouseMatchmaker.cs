namespace dsstats.shared.InHouse;

public sealed class InHouseMatchmakingSuggestion
{
    public List<InHouseMatchmakingPlayer> Team1 { get; set; } = [];
    public List<InHouseMatchmakingPlayer> Team2 { get; set; } = [];
    public List<InHouseMatchmakingPlayer> Bench { get; set; } = [];
    public List<InHouseMatchmakingPlayer> Sitters { get; set; } = [];
    public InHouseMatchmakingScores Scores { get; set; } = new();
    public List<string> Warnings { get; set; } = [];
    public bool IsComplete => Team1.Count == 3 && Team2.Count == 3;
}

public sealed class InHouseMatchmakingScores
{
    public double Team1Rating { get; set; }
    public double Team2Rating { get; set; }
    public double BalanceScore { get; set; }
    public int LeastPlayedScore { get; set; }
    public int SameRosterScore { get; set; }
    public double WeightedScore { get; set; }
}

public sealed class InHouseMatchmakingPlayer
{
    public Guid RosterPlayerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public ToonIdDto ToonId { get; set; } = new();
    public double InitialRating { get; set; }
    public int Games { get; set; }
    public int Observes { get; set; }
    public int PlayDebt { get; set; }
    public bool IsSitter { get; set; }
    public bool IsNewManual { get; set; }
    public bool PlayedLatestGame { get; set; }
    public bool ObservedLatestGame { get; set; }
}

public static class InHouseMatchmaker
{
    public const int TeamSize = 3;
    private const double SameRosterWeight = 100;

    public static InHouseMatchmakingSuggestion CreateSuggestion(InHouseGameSessionDetailDto session)
    {
        var players = CreatePlayers(session)
            .OrderByDescending(player => player.IsNewManual)
            .ThenByDescending(player => player.ObservedLatestGame)
            .ThenByDescending(player => player.PlayDebt)
            .ThenBy(player => player.Games)
            .ThenBy(player => player.Name)
            .ThenBy(player => player.RosterPlayerId)
            .ToList();

        var sitters = players.Where(player => player.IsSitter).ToList();
        var candidates = players.Where(player => !player.IsSitter).ToList();
        var selected = candidates.Take(TeamSize * 2).ToList();
        var bench = candidates.Skip(TeamSize * 2).ToList();
        var warnings = CreateWarnings(selected, candidates, session.Replays.Count);
        var pairCounts = CreateSameTeamPairCounts(session);

        var suggestion = new InHouseMatchmakingSuggestion
        {
            Bench = bench,
            Sitters = sitters,
            Warnings = warnings,
        };

        if (selected.Count < TeamSize * 2)
        {
            suggestion.Team1 = selected.Take(TeamSize).ToList();
            suggestion.Team2 = selected.Skip(TeamSize).Take(TeamSize).ToList();
            suggestion.Scores = ScoreTeams(suggestion.Team1, suggestion.Team2, pairCounts);
            return suggestion;
        }

        var best = GetBestSplit(selected, pairCounts);
        suggestion.Team1 = best.Team1;
        suggestion.Team2 = best.Team2;
        suggestion.Scores = best.Scores;
        return suggestion;
    }

    public static InHouseMatchmakingScores ScoreDraft(
        InHouseGameSessionDetailDto session,
        IReadOnlyCollection<Guid> team1RosterIds,
        IReadOnlyCollection<Guid> team2RosterIds)
    {
        var players = CreatePlayers(session).ToDictionary(player => player.RosterPlayerId);
        var team1 = team1RosterIds
            .Where(players.ContainsKey)
            .Select(id => players[id])
            .ToList();
        var team2 = team2RosterIds
            .Where(players.ContainsKey)
            .Select(id => players[id])
            .ToList();

        return ScoreTeams(team1, team2, CreateSameTeamPairCounts(session));
    }

    public static List<InHouseMatchmakingPlayer> CreatePlayers(InHouseGameSessionDetailDto session)
    {
        var replayCount = session.Replays.Count;
        return session.RosterPlayers
            .Select(player =>
            {
                var eligibleGames = Math.Max(0, replayCount - player.JoinedReplayCount);
                return new InHouseMatchmakingPlayer
                {
                    RosterPlayerId = player.RosterPlayerId,
                    Name = player.Name,
                    ToonId = player.ToonId,
                    InitialRating = player.InitialRating,
                    Games = player.Games,
                    Observes = player.Observes,
                    PlayDebt = eligibleGames - player.Games,
                    IsSitter = player.IsSitter,
                    IsNewManual = player.IsManual && player.JoinedReplayCount >= replayCount,
                    PlayedLatestGame = player.PlayedLatestGame,
                    ObservedLatestGame = player.ObservedLatestGame,
                };
            })
            .ToList();
    }

    private static List<string> CreateWarnings(
        IReadOnlyCollection<InHouseMatchmakingPlayer> selected,
        IReadOnlyCollection<InHouseMatchmakingPlayer> candidates,
        int replayCount)
    {
        List<string> warnings = [];
        if (candidates.Count < TeamSize * 2)
        {
            warnings.Add($"Need {TeamSize * 2 - candidates.Count} more available player(s) for a full 3v3.");
        }

        var mustPlayCount = candidates.Count(player => player.IsNewManual || player.ObservedLatestGame);
        if (mustPlayCount > TeamSize * 2)
        {
            warnings.Add($"{mustPlayCount} players qualify as new/observer priority, but only {TeamSize * 2} can play.");
        }

        if (replayCount == 0 && selected.Count == TeamSize * 2)
        {
            warnings.Add("No uploaded games yet; the first suggestion uses manual joins and ratings only.");
        }

        return warnings;
    }

    private static (List<InHouseMatchmakingPlayer> Team1, List<InHouseMatchmakingPlayer> Team2, InHouseMatchmakingScores Scores)
        GetBestSplit(List<InHouseMatchmakingPlayer> selected, Dictionary<PairKey, int> pairCounts)
    {
        (List<InHouseMatchmakingPlayer> Team1, List<InHouseMatchmakingPlayer> Team2, InHouseMatchmakingScores Scores)? best = null;
        var firstPlayer = selected[0].RosterPlayerId;

        foreach (var team1 in GetCombinations(selected, TeamSize))
        {
            if (!team1.Any(player => player.RosterPlayerId == firstPlayer))
            {
                continue;
            }

            var team1Ids = team1.Select(player => player.RosterPlayerId).ToHashSet();
            var team2 = selected.Where(player => !team1Ids.Contains(player.RosterPlayerId)).ToList();
            var scores = ScoreTeams(team1, team2, pairCounts);

            if (best is null
                || scores.WeightedScore < best.Value.Scores.WeightedScore
                || (scores.WeightedScore == best.Value.Scores.WeightedScore
                    && string.CompareOrdinal(GetTieBreakName(team1), GetTieBreakName(best.Value.Team1)) < 0))
            {
                best = (team1, team2, scores);
            }
        }

        return best ?? (selected.Take(TeamSize).ToList(), selected.Skip(TeamSize).ToList(), new());
    }

    private static InHouseMatchmakingScores ScoreTeams(
        IReadOnlyCollection<InHouseMatchmakingPlayer> team1,
        IReadOnlyCollection<InHouseMatchmakingPlayer> team2,
        Dictionary<PairKey, int> pairCounts)
    {
        var team1Rating = team1.Sum(player => player.InitialRating);
        var team2Rating = team2.Sum(player => player.InitialRating);
        var balanceScore = Math.Abs(team1Rating - team2Rating);
        var sameRosterScore = CountSameRosterPairs(team1, pairCounts) + CountSameRosterPairs(team2, pairCounts);
        var leastPlayedScore = team1.Concat(team2).Sum(player => Math.Max(0, player.PlayDebt));

        return new()
        {
            Team1Rating = team1Rating,
            Team2Rating = team2Rating,
            BalanceScore = balanceScore,
            LeastPlayedScore = leastPlayedScore,
            SameRosterScore = sameRosterScore,
            WeightedScore = balanceScore + sameRosterScore * SameRosterWeight,
        };
    }

    private static int CountSameRosterPairs(
        IReadOnlyCollection<InHouseMatchmakingPlayer> team,
        Dictionary<PairKey, int> pairCounts)
    {
        var count = 0;
        var teamPlayers = team.ToList();
        for (var i = 0; i < teamPlayers.Count; i++)
        {
            for (var j = i + 1; j < teamPlayers.Count; j++)
            {
                count += pairCounts.GetValueOrDefault(new PairKey(teamPlayers[i].RosterPlayerId, teamPlayers[j].RosterPlayerId));
            }
        }

        return count;
    }

    private static Dictionary<PairKey, int> CreateSameTeamPairCounts(InHouseGameSessionDetailDto session)
    {
        var rosterIdsByToon = session.RosterPlayers
            .ToDictionary(player => new ToonKey(player.ToonId), player => player.RosterPlayerId);
        Dictionary<PairKey, int> pairCounts = [];

        foreach (var replay in session.Replays)
        {
            foreach (var team in replay.Players
                .Where(player => !player.Observer && player.TeamId > 0)
                .GroupBy(player => player.TeamId))
            {
                var teamRosterIds = team
                    .Select(player => rosterIdsByToon.GetValueOrDefault(new ToonKey(player.ToonId)))
                    .Where(id => id != Guid.Empty)
                    .Distinct()
                    .OrderBy(id => id)
                    .ToList();

                for (var i = 0; i < teamRosterIds.Count; i++)
                {
                    for (var j = i + 1; j < teamRosterIds.Count; j++)
                    {
                        var key = new PairKey(teamRosterIds[i], teamRosterIds[j]);
                        pairCounts[key] = pairCounts.GetValueOrDefault(key) + 1;
                    }
                }
            }
        }

        return pairCounts;
    }

    private static IEnumerable<List<InHouseMatchmakingPlayer>> GetCombinations(
        IReadOnlyList<InHouseMatchmakingPlayer> players,
        int count)
    {
        return GetCombinations(players, count, 0, []);
    }

    private static IEnumerable<List<InHouseMatchmakingPlayer>> GetCombinations(
        IReadOnlyList<InHouseMatchmakingPlayer> players,
        int count,
        int start,
        List<InHouseMatchmakingPlayer> current)
    {
        if (current.Count == count)
        {
            yield return [.. current];
            yield break;
        }

        for (var i = start; i <= players.Count - (count - current.Count); i++)
        {
            current.Add(players[i]);
            foreach (var combination in GetCombinations(players, count, i + 1, current))
            {
                yield return combination;
            }
            current.RemoveAt(current.Count - 1);
        }
    }

    private static string GetTieBreakName(IEnumerable<InHouseMatchmakingPlayer> players)
        => string.Join("|", players.Select(player => player.Name).Order());

    private readonly record struct PairKey
    {
        public PairKey(Guid first, Guid second)
        {
            if (first.CompareTo(second) <= 0)
            {
                First = first;
                Second = second;
            }
            else
            {
                First = second;
                Second = first;
            }
        }

        public Guid First { get; }
        public Guid Second { get; }
    }

    private readonly record struct ToonKey(int Region, int Realm, int Id)
    {
        public ToonKey(ToonIdDto toonId) : this(toonId.Region, toonId.Realm, toonId.Id)
        {
        }
    }
}
