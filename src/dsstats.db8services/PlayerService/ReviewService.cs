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
        PlayerId playerId = new(request.RequestName.ToonId, request.RequestName.RealmId, request.RequestName.RegionId);
        int year = 2023;
        RatingType ratingType = request.RatingType;

        var isUploader = await IsUploader(playerId);

        if (!isUploader)
        {
            return new()
            {
                IsUploader = false
            };
        }

        (var winStreak, var losStreak) = await GetLongestStreak(playerId, token);
        var cmdrsPlayed = await GetPlayerIdCommandersPlayed(playerId, ratingType, year, token);
        var ratingInfos = await GetPlayerRatingChartData(playerId, ratingType, year, token);
        (var longestReplay, var mostCompetitiveReplay) = await GetLongestAndMostCompetitiveReplayHash(playerId, year, token);

        return new()
        {
            RatingType = request.RatingType,
            TotalGames = await GetTotalGames(playerId, year, token),
            LongestWinStreak = winStreak,
            LongestLosStreak = losStreak,
            CommanderInfos = cmdrsPlayed,
            RatingInfos = ratingInfos,
            LongestReplay = longestReplay,
            MostCompetitiveReplay = mostCompetitiveReplay,
            GreatestComebackReplay = await GetGreatestComebackReplayHash(playerId, year, token),
            IsUploader = true
        };
    }

    public async Task<ReviewResponse> GetReviewRatingTypeInfo(ReviewRequest request, CancellationToken token = default)
    {
        PlayerId playerId = new(request.RequestName.ToonId, request.RequestName.RealmId, request.RequestName.RegionId);
        int year = 2023;
        RatingType ratingType = request.RatingType;

        var isUploader = await IsUploader(playerId);

        if (!isUploader)
        {
            return new()
            {
                IsUploader = false
            };
        }

        var cmdrsPlayed = await GetPlayerIdCommandersPlayed(playerId, ratingType, year, token);
        var ratingInfos = await GetPlayerRatingChartData(playerId, ratingType, year, token);
        return new()
        {
            RatingType = request.RatingType,
            CommanderInfos = cmdrsPlayed,
            RatingInfos = ratingInfos,
            IsUploader = true
        };
    }

    private async Task<bool> IsUploader(PlayerId playerId)
    {
        var uploaderId = await context.Players
            .Where(x => x.ToonId == playerId.ToonId
                && x.RealmId == playerId.RealmId
                && x.RegionId == playerId.RegionId)
            .Select(s => s.UploaderId)
            .FirstOrDefaultAsync();

        return uploaderId > 0;
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

    private async Task<(string?, string?)> GetLongestAndMostCompetitiveReplayHash(PlayerId playerId, int year, CancellationToken token)
    {
        (var fromDate, var toDate) = GetFromTo(year);

        var query = from p in context.Players
                    from rp in p.ReplayPlayers
                    join r in context.Replays on rp.ReplayId equals r.ReplayId
                    where r.GameTime >= fromDate && r.GameTime < toDate
                     && p.ToonId == playerId.ToonId
                     && p.RealmId == playerId.RealmId
                     && p.RegionId == playerId.RegionId
                     && r.DefaultFilter
                    select r;

        var longestReplay = await query.OrderByDescending(o => o.Duration)
            .Select(s => s.ReplayHash)
            .FirstOrDefaultAsync(token);

        var mostCompetitiveReplay = await query.OrderByDescending(o => o.Middle.Length)
            .Select(s => s.ReplayHash)
            .FirstOrDefaultAsync(token);

        return (longestReplay, mostCompetitiveReplay);
    }

    private async Task<string?> GetGreatestComebackReplayHash(PlayerId playerId, int year, CancellationToken token)
    {
        (var fromDate, var toDate) = GetFromTo(year);

        var query = from p in context.Players
                    from rp in p.ReplayPlayers
                    join r in context.Replays on rp.ReplayId equals r.ReplayId
                    where r.GameTime >= fromDate && r.GameTime < toDate
                     && p.ToonId == playerId.ToonId
                     && p.RealmId == playerId.RealmId
                     && p.RegionId == playerId.RegionId
                     && r.DefaultFilter
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

