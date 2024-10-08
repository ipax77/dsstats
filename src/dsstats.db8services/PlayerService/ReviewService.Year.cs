﻿using dsstats.shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace dsstats.db8services;

public partial class ReviewService
{
    public async Task<ReviewYearResponse> GetYearReview(RatingType ratingType, int year, CancellationToken token = default)
    {
        var memKey = $"review{ratingType}{year}";

        if (!memoryCache.TryGetValue(memKey, out ReviewYearResponse? response)
            || response is null)
        {
            (var longestReplay, var mostCompetitiveReplay) = await GetLongestAndMostCompetitiveYearReplayHash(ratingType, year, token);

            response = new()
            {
                RatingType = ratingType,
                TotalGames = await GetYearTotalGames(ratingType, year, token),
                CommanderInfos = await GetYearCommandersPlayed(ratingType, year, token),
                LongestReplay = longestReplay,
                MostCompetitiveReplay = mostCompetitiveReplay,
                GreatestComebackReplay = await GetGreatestComebackYearReplayHash(ratingType, year, token),
            };

            memoryCache.Set(memKey, response, TimeSpan.FromHours(24));
        }
        return response;
    }

    public async Task<ReviewYearResponse> GetYearRatingTypeReview(RatingType ratingType, int year, CancellationToken token = default)
    {
        return await GetYearReview(ratingType, year, token);
    }

    private async Task<int> GetYearTotalGames(RatingType ratingType, int year, CancellationToken token)
    {
        (var fromDate, var toDate) = GetFromTo(year);
        bool withoutRating = ratingType == RatingType.None;

        return await context.Replays
            .Where(x => x.GameTime >= fromDate
                && x.GameTime < toDate
                && (withoutRating || (x.ReplayRatingInfo != null && x.ReplayRatingInfo.RatingType == ratingType)))
            .CountAsync(token);
    }

    private async Task<List<CommanderReviewInfo>> GetYearCommandersPlayed(RatingType ratingType,
                                                                          int year,
                                                                          CancellationToken token)
    {
        (var fromDate, var toDate) = GetFromTo(year);

        IQueryable<CommanderReviewInfo> query;

        if (ratingType == RatingType.None)
        {
            query = from r in context.Replays
                    from rp in r.ReplayPlayers
                    where r.GameTime >= fromDate && r.GameTime < toDate
                     && r.ReplayRatingInfo == null
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
            query = from r in context.Replays
                    from rp in r.ReplayPlayers
                    join rr in context.ReplayRatings on rp.ReplayId equals rr.ReplayId
                    where r.GameTime >= fromDate && r.GameTime < toDate
                     && rr.RatingType == ratingType
                    group new { rp } by new { rp.Race } into g
                    orderby g.Count() descending
                    select new CommanderReviewInfo()
                    {
                        Cmdr = g.Key.Race,
                        Count = g.Count(),
                    };
        }
        var data = await query.ToListAsync(token);
        if (ratingType == RatingType.Std || ratingType == RatingType.StdTE)
        {
            data = data.Where(x => (int)x.Cmdr <= 3 && x.Cmdr != Commander.None).ToList();
        }
        else if (ratingType == RatingType.Cmdr || ratingType == RatingType.CmdrTE)
        {
            data = data.Where(x => (int)x.Cmdr > 3).ToList();
        }

        return data;
    }

    private async Task<(string?, string?)> GetLongestAndMostCompetitiveYearReplayHash(RatingType ratingType, int year, CancellationToken token)
    {
        (var fromDate, var toDate) = GetFromTo(year);

        var query = ratingType == RatingType.None ?
            from r in context.Replays
            from rp in r.ReplayPlayers
            where r.GameTime >= fromDate && r.GameTime < toDate
             && r.DefaultFilter
            select r
            : from r in context.Replays
              from rp in r.ReplayPlayers
              where r.GameTime >= fromDate && r.GameTime < toDate
               && r.DefaultFilter
               && (r.ReplayRatingInfo != null && r.ReplayRatingInfo.RatingType == ratingType)
              select r;

        var longestReplay = await query.OrderByDescending(o => o.Duration)
            .Select(s => s.ReplayHash)
            .FirstOrDefaultAsync(token);

        var mostCompetitiveReplay = await query.OrderByDescending(o => o.Middle.Length)
            .Select(s => s.ReplayHash)
            .FirstOrDefaultAsync(token);

        return (longestReplay, mostCompetitiveReplay);
    }

    private async Task<string?> GetGreatestComebackYearReplayHash(RatingType ratingType, int year, CancellationToken token)
    {
        (var fromDate, var toDate) = GetFromTo(year);

        var query = ratingType == RatingType.None ?
            from r in context.Replays
            from rp in r.ReplayPlayers
            where r.GameTime >= fromDate && r.GameTime < toDate
             && r.DefaultFilter
            select rp
            : from r in context.Replays
              from rp in r.ReplayPlayers
              where r.GameTime >= fromDate && r.GameTime < toDate
                  && r.DefaultFilter
                  && (r.ReplayRatingInfo != null && r.ReplayRatingInfo.RatingType == ratingType)
              select rp;

        var infos = from rp in query
                    where rp.PlayerResult == PlayerResult.Win
                        && rp.Replay.Duration > 360
                        && rp.Replay.Middle.Length > 0
                    select new
                    {
                        rp.Replay.ReplayHash,
                        rp.Replay.Middle,
                        rp.Replay.Duration,
                        rp.Replay.WinnerTeam
                    };
        var linfos = await infos.ToListAsync(token);

        double minMid = 100;
        string? replayHash = null;
        foreach (var info in linfos)
        {
            (int startTeam, int[] gameloops, int totalGameloops) = GetMiddleInfo(info.Middle, info.Duration);
            (var mid1, var mid2) = GetChartMiddle(startTeam, gameloops, totalGameloops);

            var plMid = info.WinnerTeam == 1 ? mid1 : mid2;
            if (plMid > 0 && plMid < minMid)
            {
                minMid = plMid;
                replayHash = info.ReplayHash;
            }
        }

        return replayHash;
    }
}
