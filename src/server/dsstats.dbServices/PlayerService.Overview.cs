using dsstats.db;
using dsstats.shared;
using dsstats.shared.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace dsstats.dbServices;

public partial class PlayerService : IPlayerProfileService
{
    public Task<CmdrAvgGainResponse> GetCommanderPerformance(PlayerStatsRequest request, CancellationToken token = default) =>
        GetCommandersPerformance(request, token);

    public async Task<PlayerOverview?> GetOverview(PlayerProfileRequest request, CancellationToken token = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(token);
        int? playerId = await FindProfilePlayer(context, request, token);
        if (playerId is null)
            return null;

        var profile = await GetBasicPlayerStats(playerId.Value, context, token);
        // Only the selected player's participation and rating are loaded across their history.
        var history = await (
            from rp in context.ReplayPlayers
            join rr in context.ReplayRatings on rp.ReplayId equals rr.ReplayId
            from rating in context.ReplayPlayerRatings
                .Where(pr => pr.ReplayRatingId == rr.ReplayRatingId && pr.ReplayPlayerId == rp.ReplayPlayerId)
                .DefaultIfEmpty()
            where rp.PlayerId == playerId.Value && rr.RatingType == request.RatingType
            orderby rp.Replay!.Gametime, rp.ReplayId
            select new
            {
                rp.ReplayId, rp.Replay!.Gametime, rp.Replay.WinnerTeam, rp.TeamId,
                Before = rating == null ? (double?)null : rating.RatingBefore,
                Delta = rating == null ? 0 : rating.RatingDelta,
                Games = rating == null ? 0 : rating.Games
            }).TagWith("PlayerOverview: self history").AsNoTracking().ToListAsync(token);

        var replays = new List<PlayerReplayData>(history.Count);
        var ratings = new Dictionary<int, PlayerReplayRatingData>(history.Count);
        foreach (var row in history)
        {
            replays.Add(new()
            {
                ReplayId = row.ReplayId, Gametime = row.Gametime, WinnerTeam = row.WinnerTeam,
                Players = [new() { PlayerId = playerId.Value, TeamId = row.TeamId }]
            });
            if (row.Before is { } before)
                ratings[row.ReplayId] = new()
                {
                    PlayerRatings = [new()
                    {
                        PlayerId = playerId.Value, RatingBefore = before,
                        RatingDelta = row.Delta, Games = row.Games
                    }]
                };
        }
        var calculated = PlayerRatingDetailsCalculator.Calculate(replays, ratings, playerId.Value,
            includeRecent: false, includeRelationships: false);

        var recentKeys = await context.ReplayPlayers
            .Where(p => p.PlayerId == playerId.Value && p.Replay!.Ratings.Any(r => r.RatingType == request.RatingType))
            .OrderByDescending(p => p.Replay!.Gametime).ThenByDescending(p => p.ReplayId)
            .Take(12).Select(p => p.ReplayId).ToListAsync(token);
        var recentIds = context.Replays.Where(r => recentKeys.Contains(r.ReplayId)).Select(r => r.ReplayId);
        var recentReplays = await LoadReplays(context, request.RatingType, recentIds, token);
        var recentRatings = await LoadReplayRatings(context, request.RatingType, recentIds, token);
        var recent = PlayerRatingDetailsCalculator.Calculate(recentReplays, recentRatings, playerId.Value,
            includeHistory: false, includeRelationships: false);

        return new()
        {
            ToonId = request.ToonId, Name = profile!.Name, RegionId = profile.RegionId,
            RatingType = request.RatingType, Ratings = profile.Ratings,
            PercentileMaxRank = await GetPercentileMaxRank(request.RatingType, context, token),
            History = calculated.Ratings, CurrentStreak = calculated.CurrentStreak,
            LongestWinStreak = calculated.LongestWinStreak, LongestLoseStreak = calculated.LongestLoseStreak,
            TopRating = calculated.TopRating, Replays = recent.Replays
        };
    }

    public async Task<PlayerProfileDetails?> GetDetails(PlayerProfileRequest request, CancellationToken token = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(token);
        int? playerId = await FindProfilePlayer(context, request, token);
        if (playerId is null)
            return null;

        var replayIds = context.ReplayPlayers.Where(p => p.PlayerId == playerId.Value).Select(p => p.ReplayId);
        var replays = await LoadReplays(context, request.RatingType, replayIds, token, detailsOnly: true);
        var ratings = await LoadReplayRatings(context, request.RatingType, replayIds, token, detailsOnly: true);
        var calculated = PlayerRatingDetailsCalculator.Calculate(replays, ratings, playerId.Value,
            includeHistory: false, includeRecent: false);
        foreach (var stat in calculated.TeammateStats.Concat(calculated.OpponentStats))
            stat.Player.Name = importService.GetPlayerName(stat.Player.ToonId);

        return new()
        {
            RatingType = request.RatingType, GameModes = calculated.GameModes,
            Commanders = calculated.Commanders, Positions = calculated.PosStats,
            Teammates = calculated.TeammateStats, Opponents = calculated.OpponentStats,
            AvgTeammateRating = calculated.AvgTeammateRating, AvgOpponentRating = calculated.AvgOpponentRating,
            CommanderPerformance = calculated.AvgGainResponses.Single()
        };
    }

    private static Task<int?> FindProfilePlayer(DsstatsContext context, PlayerProfileRequest request, CancellationToken token) =>
        context.Players.Where(p => p.ToonId.Id == request.ToonId.Id
            && p.ToonId.Region == request.ToonId.Region && p.ToonId.Realm == request.ToonId.Realm)
            .Select(p => (int?)p.PlayerId).FirstOrDefaultAsync(token);
}
