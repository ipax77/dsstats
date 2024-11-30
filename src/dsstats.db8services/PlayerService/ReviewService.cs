using dsstats.db8;
using dsstats.shared;
using dsstats.shared.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace dsstats.db8services;

public partial class ReviewService(ReplayContext context, IMemoryCache memoryCache, ILogger<ReviewService> logger) : IReviewService
{
    public async Task<ReviewResponse> GetReview(ReviewRequest request, CancellationToken token = default)
    {
        var memKey = $"reviewpl{request.Year}{request.RatingType}{request.RequestName.Name}{request.RequestName.ToonId}|{request.RequestName.RealmId}|{request.RequestName.RegionId}";

        if (!memoryCache.TryGetValue(memKey, out ReviewResponse? response)
            || response is null)
        {
            response = await ProduceReview(request, token);
            memoryCache.Set(memKey, response, TimeSpan.FromHours(24));
        }
        return response;
    }

    public async Task<ReviewResponse> GetReviewRatingTypeInfo(ReviewRequest request, CancellationToken token = default)
    {
        return await GetReview(request, token);
    }

    private async Task<ReviewResponse> ProduceReview(ReviewRequest request, CancellationToken token = default)
    {
        RatingType ratingType = request.RatingType;

        var playerId = await context.Players
            .Where(x => x.ToonId == request.RequestName.ToonId
                && x.RealmId == request.RequestName.RealmId
                && x.RegionId == request.RequestName.RegionId)
            .Select(s => s.PlayerId)
            .FirstOrDefaultAsync(token);

        var isUploader = await IsUploader(playerId);

        if (!isUploader)
        {
            return new()
            {
                IsUploader = false
            };
        }

        var ratingInfos = await GetPlayerRatingChartData(playerId, ratingType, request.Year, token);
        (var longestReplay, var mostCompetitiveReplay) = await GetLongestAndMostCompetitiveReplayHash(playerId, ratingType, request.Year, token);

        ReviewResponse response = new()
        {
            RatingType = request.RatingType,
            RatingInfos = ratingInfos,
            LongestReplay = longestReplay,
            MostCompetitiveReplay = mostCompetitiveReplay,
            GreatestComebackReplay = await GetGreatestComebackReplayHash(playerId, ratingType, request.Year, token),
            IsUploader = true
        };
        await SetLongestStreaks(playerId, ratingType, request.Year, response, token);
        return response;
    }

    private async Task<bool> IsUploader(int playerId)
    {
        if (playerId == 0)
        {
            return false;
        }
        var uploaderId = await context.Players
            .Where(x => x.PlayerId == playerId)
            .Select(s => s.UploaderId)
            .FirstOrDefaultAsync();
        return uploaderId > 0;
    }

    private async Task<List<ReplayPlayerReviewDto>> GetPlayerRatingChartData(int playerId,
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
            query = from rp in context.ReplayPlayers
                    join r in context.Replays on rp.ReplayId equals r.ReplayId
                    join rr in context.ReplayRatings on r.ReplayId equals rr.ReplayId
                    join rpr in context.RepPlayerRatings on rp.ReplayPlayerId equals rpr.ReplayPlayerId
                    where r.GameTime >= fromDate
                     && r.GameTime < toDate
                     && rr.RatingType == ratingType
                     && rp.PlayerId == playerId
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
            query = from rp in context.ReplayPlayers
                    join r in context.Replays on rp.ReplayId equals r.ReplayId
                    join rr in context.ComboReplayRatings on r.ReplayId equals rr.ReplayId
                    join rpr in context.ComboReplayPlayerRatings on rp.ReplayPlayerId equals rpr.ReplayPlayerId
                    where r.GameTime >= fromDate
                     && r.GameTime < toDate
                     && rr.RatingType == ratingType
                     && rp.PlayerId == playerId
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

    private async Task SetLongestStreaks(int playerId,
                                         RatingType ratingType,
                                         int year,
                                         ReviewResponse response,
                                         CancellationToken token)
    {
        (var fromDate, var toDate) = GetFromTo(year);
        try
        {
            var replayInfos = ratingType == RatingType.None
                ? from r in context.Replays
                  from rp in r.ReplayPlayers
                  where rp.PlayerId == playerId
                    && r.GameTime >= fromDate
                    && r.GameTime < toDate
                  orderby r.GameTime
                  select new { rp.PlayerResult, rp.Race, r.Duration }
                : from r in context.Replays
                  from rp in r.ReplayPlayers
                  where rp.PlayerId == playerId
                      && r.GameTime >= fromDate
                      && r.GameTime < toDate
                      && (r.ComboReplayRating != null && r.ComboReplayRating.RatingType == ratingType)
                  orderby r.GameTime
                  select new { rp.PlayerResult, rp.Race, r.Duration };


            var results = await replayInfos.ToListAsync(token);

            int winStreak = 0;
            int loseStreak = 0;
            int currentStreak = 0;
            long duration = 0;
            Dictionary<Commander, int> cmdrCounts = [];

            for (int i = 0; i < results.Count; i++)
            {
                var result = results[i];

                duration += result.Duration;
                if (!cmdrCounts.ContainsKey(result.Race))
                {
                    cmdrCounts[result.Race] = 1;
                }
                else
                {
                    cmdrCounts[result.Race]++;
                }

                if (result.PlayerResult == PlayerResult.Win)
                {
                    if (currentStreak > 0)
                    {
                        currentStreak++;
                        if (currentStreak > winStreak)
                        {
                            winStreak = currentStreak;
                        }
                    }
                    else
                    {
                        currentStreak = 1;
                    }
                }
                else
                {
                    if (currentStreak < 0)
                    {
                        currentStreak--;
                        if (Math.Abs(currentStreak) > loseStreak)
                        {
                            loseStreak = Math.Abs(currentStreak);
                        }
                    }
                    else
                    {
                        currentStreak = -1;
                    }
                }
            }
            response.LongestWinStreak = winStreak;
            response.LongestLosStreak = loseStreak;
            response.CurrentStreak = currentStreak;
            response.CommanderInfos = cmdrCounts
                .Select(s => new CommanderReviewInfo() { Cmdr = s.Key, Count = s.Value, RatingType = ratingType })
                .ToList();
            response.Duration = Convert.ToInt32(TimeSpan.FromSeconds(duration).TotalMinutes);
            response.TotalGames = results.Count;
        }
        catch (Exception ex)
        {
            logger.LogError("failed getting player streaks: {error}", ex.Message);
        }
    }

    private async Task<(string?, string?)> GetLongestAndMostCompetitiveReplayHash(int playerId, RatingType ratingType, int year, CancellationToken token)
    {
        (var fromDate, var toDate) = GetFromTo(year);

        var query = ratingType == RatingType.None ?
            from rp in context.ReplayPlayers
            join r in context.Replays on rp.ReplayId equals r.ReplayId
            where r.GameTime >= fromDate && r.GameTime < toDate
             && rp.PlayerId == playerId
             && r.DefaultFilter
            select r
            : from rp in context.ReplayPlayers
              join r in context.Replays on rp.ReplayId equals r.ReplayId
              where r.GameTime >= fromDate && r.GameTime < toDate
               && rp.PlayerId == playerId
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

    private async Task<string?> GetGreatestComebackReplayHash(int playerId, RatingType ratingType, int year, CancellationToken token)
    {
        (var fromDate, var toDate) = GetFromTo(year);

        var query = ratingType == RatingType.None ?
            from rp in context.ReplayPlayers
            join r in context.Replays on rp.ReplayId equals r.ReplayId
            where r.GameTime >= fromDate && r.GameTime < toDate
             && rp.PlayerId == playerId
             && r.DefaultFilter
            select rp
            : from rp in context.ReplayPlayers
              join r in context.Replays on rp.ReplayId equals r.ReplayId
              where r.GameTime >= fromDate && r.GameTime < toDate
              && rp.PlayerId == playerId
              && r.DefaultFilter
              && (r.ReplayRatingInfo != null && r.ReplayRatingInfo.RatingType == ratingType)
              select rp;

        var infos = from rp in query
                    where rp.Replay.DefaultFilter
                        && rp.PlayerResult == PlayerResult.Win
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

    private (DateTime, DateTime) GetFromTo(int year)
    {
        var from = new DateTime(year, 1, 1);
        var to = new DateTime(year + 1, 1, 1);
        return (from, to);
    }

    private static (int, int[], int) GetMiddleInfo(string middleString, int duration)
    {
        int totalGameloops = (int)(duration * 22.4);

        if (!String.IsNullOrEmpty(middleString))
        {
            var ents = middleString.Split('|').Where(x => !String.IsNullOrEmpty(x)).ToArray();
            var ients = ents.Select(s => int.Parse(s)).ToList();
            ients.Add(totalGameloops);
            int startTeam = ients[0];
            ients.RemoveAt(0);
            return (startTeam, ients.ToArray(), totalGameloops);
        }
        return (0, Array.Empty<int>(), totalGameloops);
    }

    private static (double, double) GetChartMiddle(int startTeam, int[] gameloops, int gameloop)
    {
        if (gameloops.Length < 2)
        {
            return (0, 0);
        }

        int sumTeam1 = 0;
        int sumTeam2 = 0;
        bool isFirstTeam = startTeam == 1;
        int lastLoop = 0;
        bool hasInfo = false;

        for (int i = 0; i < gameloops.Length; i++)
        {
            if (lastLoop > gameloop)
            {
                hasInfo = true;
                break;
            }

            isFirstTeam = !isFirstTeam;
            if (lastLoop > 0)
            {
                if (isFirstTeam)
                {
                    sumTeam1 += gameloops[i] - lastLoop;
                }
                else
                {
                    sumTeam2 += gameloops[i] - lastLoop;
                }
            }
            lastLoop = gameloops[i];
        }

        if (hasInfo)
        {
            if (isFirstTeam)
            {
                sumTeam1 -= lastLoop - gameloop;
            }
            else
            {
                sumTeam2 -= lastLoop - gameloop;
            }
        }
        else if (gameloops.Length > 0)
        {
            if (isFirstTeam)
            {
                sumTeam1 -= gameloops[^1] - gameloop;
            }
            else
            {
                sumTeam2 -= gameloops[^1] - gameloop;
            }
        }

        sumTeam1 = Math.Max(sumTeam1, 0);
        sumTeam2 = Math.Max(sumTeam2, 0);

        return (Math.Round(sumTeam1 * 100.0 / (double)gameloops[^1], 2), Math.Round(sumTeam2 * 100.0 / (double)gameloops[^1], 2));
    }
}

