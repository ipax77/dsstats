using dsstats.db8;
using dsstats.shared;
using dsstats.shared.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace dsstats.db8services;

public class ReviewService(ReplayContext context, ILogger<ReviewService> logger) : IReviewService
{
    public async Task<ReviewResponse> GetReview(ReviewRequest request, CancellationToken token = default)
    {
        PlayerId playerId = new(226401, 1, 2);
        int year = 2023;
        RatingType ratingType = request.RatingType;

        (var winStreak, var losStreak) = await GetLongestStreak(playerId, token);
        var cmdrsPlayed = await GetPlayerIdCommandersPlayed(playerId, ratingType, year, token);
        var ratingInfos = await GetPlayerRatingChartData(playerId, ratingType, year, token);

        return new()
        {
            RatingType = request.RatingType,
            TotalGames = await GetTotalGames(playerId, year, token),
            LongestWinStreak = winStreak,
            LongestLosStreak = losStreak,
            CommanderInfos = cmdrsPlayed,
            RatingInfos = ratingInfos,
        };
    }

    private async Task<int> GetTotalGames(PlayerId playerId, int year, CancellationToken token)
    {
        (var fromDate, var toDate) = GetFromTo(year);

        var query = from p in context.Players
                    from rp in p.ReplayPlayers
                    join r in context.Replays on rp.ReplayId equals r.ReplayId
                    where r.GameTime >= fromDate && r.GameTime < toDate
                     && p.ToonId == playerId.ToonId
                     && p.RealmId == playerId.RealmId
                     && p.RegionId == playerId.RegionId
                    select r;
        return await query.CountAsync(token);
    }

    private async Task<int> GetWinStreak(PlayerId playerId, int year, CancellationToken token)
    {
        (var fromDate, var toDate) = GetFromTo(year);

        var query = from p in context.Players
                    from rp in p.ReplayPlayers
                    join r in context.Replays on rp.ReplayId equals r.ReplayId
                    where r.GameTime >= fromDate && r.GameTime < toDate
                     && p.ToonId == playerId.ToonId
                     && p.RealmId == playerId.RealmId
                     && p.RegionId == playerId.RegionId
                    select r;
        return await query.CountAsync(token);
    }

    private async Task<List<CommanderReviewInfo>> GetPlayerIdCommandersPlayed(PlayerId playerId,
                                                                              RatingType ratingType,
                                                                              int year,
                                                                              CancellationToken token)
    {
        (var fromDate, var toDate) = GetFromTo(year);

        IQueryable<CommanderReviewInfo> query;

        if (ratingType == RatingType.None)
        {
            query = from p in context.Players
                    from rp in p.ReplayPlayers
                    join r in context.Replays on rp.ReplayId equals r.ReplayId
                    where r.GameTime >= fromDate && r.GameTime < toDate
                     && r.ReplayRatingInfo == null
                     && p.ToonId == playerId.ToonId
                     && p.RealmId == playerId.RealmId
                     && p.RegionId == playerId.RegionId
                    group new { rp } by new { rp.Race } into g
                    orderby g.Count() descending
                    select new CommanderReviewInfo()
                    {
                        Cmdr = g.Key.Race,
                        Count = g.Count(),
                    };
        }
        else
        {
            query = from p in context.Players
                    from rp in p.ReplayPlayers
                    join r in context.Replays on rp.ReplayId equals r.ReplayId
                    join rr in context.ReplayRatings on rp.ReplayId equals rr.ReplayId
                    where r.GameTime >= fromDate && r.GameTime < toDate
                     && rr.RatingType == ratingType
                     && p.ToonId == playerId.ToonId
                     && p.RealmId == playerId.RealmId
                     && p.RegionId == playerId.RegionId
                    group new { rp } by new { rp.Race } into g
                    orderby g.Count() descending
                    select new CommanderReviewInfo()
                    {
                        Cmdr = g.Key.Race,
                        Count = g.Count(),
                    };
        }
        return await query.ToListAsync(token);
    }

    private async Task<List<ReplayPlayerReviewDto>> GetPlayerRatingChartData(PlayerId playerId,
                                                                             RatingType ratingType,
                                                                             int year,
                                                                             CancellationToken token)
    {
        if (ratingType == RatingType.None)
        {
            return new();
        }

        (var fromDate, var toDate) = GetFromTo(year);

        IQueryable<ReplayPlayerReviewDto> query;

        if ((int)ratingType >= 3)
        {
            query = from p in context.Players
                    from rp in p.ReplayPlayers
                    join r in context.Replays on rp.ReplayId equals r.ReplayId
                    join rr in context.ReplayRatings on r.ReplayId equals rr.ReplayId
                    join rpr in context.RepPlayerRatings on rp.ReplayPlayerId equals rpr.ReplayPlayerId
                    where r.GameTime >= fromDate
                     && r.GameTime < toDate
                     && rr.RatingType == ratingType
                     && p.ToonId == playerId.ToonId
                     && p.RealmId == playerId.RealmId
                     && p.RegionId == playerId.RegionId
                    group new { r, rpr } by new { r.GameTime.Year, r.GameTime.Month, r.GameTime.Day } into g
                    select new ReplayPlayerReviewDto()
                    {
                        Replay = new(g.Key.Year, g.Key.Month, g.Key.Day),
                        ReplayPlayerRatingInfo = new()
                        {
                            Rating = Math.Round(g.Max(a => a.rpr.Rating), 2),
                            Games = g.Max(m => m.rpr.Games)
                        }
                    };
        }
        else
        {
            query = from p in context.Players
                    from rp in p.ReplayPlayers
                    join r in context.Replays on rp.ReplayId equals r.ReplayId
                    join rr in context.ComboReplayRatings on r.ReplayId equals rr.ReplayId
                    join rpr in context.ComboReplayPlayerRatings on rp.ReplayPlayerId equals rpr.ReplayPlayerId
                    where r.GameTime >= fromDate
                     && r.GameTime < toDate
                     && rr.RatingType == ratingType
                     && p.ToonId == playerId.ToonId
                     && p.RealmId == playerId.RealmId
                     && p.RegionId == playerId.RegionId
                    group new { r, rr, rpr } by new { r.GameTime.Year, r.GameTime.Month, r.GameTime.Day } into g
                    select new ReplayPlayerReviewDto()
                    {
                        Replay = new(g.Key.Year, g.Key.Month, g.Key.Day),
                        ReplayPlayerRatingInfo = new()
                        {
                            Rating = g.Max(a => a.rpr.Rating),
                            Games = g.Max(m => m.rpr.Games)
                        }
                    };
        }
        return await query.ToListAsync(token);
    }

    private async Task<(int, int)> GetLongestStreak(PlayerId playerId, CancellationToken token)
    {
        try
        {
            var id = await context.Players
                .Where(x => x.ToonId == playerId.ToonId
                    && x.RealmId == playerId.RealmId
                    && x.RegionId == playerId.RegionId)
                .Select(s => s.PlayerId)
                .FirstOrDefaultAsync();

            if (id == 0)
            {
                return (0, 0);
            }
            var streaks = await context.StreakInfos
                .FromSql($"CALL GetLongest2023Streak({id});")
                .ToListAsync(token);

            (var wins, var loss) = (streaks.FirstOrDefault(f => f.PlayerResult == 1)?.LongestStreak ?? 0,
                    streaks.FirstOrDefault(f => f.PlayerResult == 2)?.LongestStreak ?? 0);
            return ((int)wins, (int)loss);
        }
        catch (Exception ex)
        {
            logger.LogError("failed setting player rating pos: {error}", ex.Message);
        }
        return (0, 0);
    }

    private (DateTime, DateTime) GetFromTo(int year)
    {
        var from = new DateTime(year, 1, 1);
        var to = new DateTime(year + 1, 1, 1);
        return (from, to);
    }

}

