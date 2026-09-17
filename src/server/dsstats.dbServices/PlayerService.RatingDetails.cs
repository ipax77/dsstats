using dsstats.db;
using dsstats.shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace dsstats.dbServices;

public partial class PlayerService
{
    public async Task<RatingDetails> GetRatingDetails(PlayerStatsRequest request, CancellationToken token = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(token);
        return await GetRatingDetails(request.RatingType, importService.GetPlayerId(request.ToonId), context, token);
    }

    private async Task<RatingDetails> GetRatingDetails(
        RatingType ratingType, int playerId, DsstatsContext context, CancellationToken token)
    {
        var replayIds = context.ReplayPlayers
            .Where(p => p.PlayerId == playerId)
            .Select(p => p.ReplayId);

        var replays = await LoadReplays(context, ratingType, replayIds, token);
        var ratings = await LoadReplayRatings(context, ratingType, replayIds, token);
        var details = PlayerRatingDetailsCalculator.Calculate(replays, ratings, playerId);
        details.RatingType = ratingType;

        foreach (var stat in details.TeammateStats.Concat(details.OpponentStats))
        {
            stat.Player.Name = importService.GetPlayerName(stat.Player.ToonId);
        }

        details.PercentileMaxRank = await GetPercentileMaxRank(ratingType, context, token);
        return details;
    }
    private static async Task<List<PlayerReplayData>> LoadReplays(DsstatsContext context, RatingType ratingType, IQueryable<int> replayIds, CancellationToken token)
    {
        return await context.Replays
            .AsNoTracking()
            .Where(r => replayIds.Contains(r.ReplayId)
                && r.Ratings.Any(a => a.RatingType == ratingType))
            .Select(s => new PlayerReplayData
            {
                ReplayId = s.ReplayId,
                ReplayHash = s.ReplayHash,
                Gametime = s.Gametime,
                GameMode = s.GameMode,
                Duration = s.Duration,
                WinnerTeam = s.WinnerTeam,
                Players = s.Players
                    .Select(p => new PlayerReplayParticipantData
                    {
                        GamePos = p.GamePos,
                        Race = p.Race,
                        PlayerId = p.PlayerId,
                        ToonId = p.Player!.ToonId,
                        TeamId = p.TeamId,
                    }).ToList(),
            })
            .OrderBy(o => o.Gametime)
            .ThenBy(o => o.ReplayId)
            .ToListAsync(token);
    }

    private static async Task<Dictionary<int, PlayerReplayRatingData>> LoadReplayRatings(DsstatsContext context, RatingType ratingType, IQueryable<int> replayIds, CancellationToken token)
    {
        return await context.ReplayRatings
            .AsNoTracking()
            .Where(r => replayIds.Contains(r.ReplayId) &&
                        r.RatingType == ratingType)
            .Select(rr => new PlayerReplayRatingData
            {
                ReplayId = rr.ReplayId,
                LeaverType = rr.LeaverType,
                ExpectedWinProbability = rr.ExpectedWinProbability,
                AvgRating = rr.AvgRating,
                PlayerRatings = rr.ReplayPlayerRatings
                    .Select(pr => new PlayerReplayParticipantRatingData
                    {
                        PlayerId = pr.PlayerId,
                        RatingBefore = pr.RatingBefore,
                        RatingDelta = pr.RatingDelta,
                        Games = pr.Games
                    })
                    .ToList()
            })
            .ToDictionaryAsync(k => k.ReplayId, v => v, token);
    }

    private async Task<int> GetPercentileMaxRank(RatingType ratingType, DsstatsContext context, CancellationToken token)
    {
        string cacheKey = $"PercentileRank_{ratingType}";
        if (!memoryCache.TryGetValue(cacheKey, out int maxRank))
        {
            maxRank = await context.PlayerRatings
                .Where(pr => pr.RatingType == ratingType)
                .OrderByDescending(pr => pr.Position)
                .Select(pr => pr.Position)
                .FirstOrDefaultAsync(token);
            memoryCache.Set(cacheKey, maxRank, TimeSpan.FromHours(20));
        }
        return maxRank;
    }
}
