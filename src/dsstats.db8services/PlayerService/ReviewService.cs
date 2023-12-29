using dsstats.db8;
using dsstats.shared;
using dsstats.shared.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace dsstats.db8services;

public class ReviewService(ReplayContext context) : IReviewService
{
    public async Task<ReviewResponse> GetReview(ReviewRequest request, CancellationToken token = default)
    {
        PlayerId playerId = new(226401, 1, 2);
        int year = 2023;

        var cmdrsPlayed = await GetPlayerIdCommandersPlayed(playerId, year, token);
        var ratingInfos = await GetDsstatsPlayerRatingChartData(playerId, year, token);

        return new()
        {
            CommanderInfos = cmdrsPlayed,
            RatingInfos = ratingInfos,
        };
    }

    private async Task<List<CommanderReviewInfo>> GetPlayerIdCommandersPlayed(PlayerId playerId, int year, CancellationToken token)
    {
        (var fromDate, var toDate) = GetFromTo(year);

#pragma warning disable CS0472 // The result of the expression is always the same since a value of this type is never equal to 'null'
        var query = from p in context.Players
                    from rp in p.ReplayPlayers
                    join r in context.Replays on rp.ReplayId equals r.ReplayId
                    join rr in context.ReplayRatings on rp.ReplayId equals rr.ReplayId into grouping
                    from rrgroup in grouping.DefaultIfEmpty()
                    where r.GameTime >= fromDate && r.GameTime < toDate
                     && p.ToonId == playerId.ToonId
                     && p.RealmId == playerId.RealmId
                     && p.RegionId == playerId.RegionId
                    group new { rp, rrgroup } by new { rp.Race, rrgroup.RatingType } into g
                    orderby g.Count() descending
                    select new CommanderReviewInfo()
                    {
                        Cmdr = g.Key.Race,
                        Count = g.Count(),
                        RatingType = g.Key.RatingType == null ? RatingType.None : g.Key.RatingType,
                    };
#pragma warning restore CS0472 // The result of the expression is always the same since a value of this type is never equal to 'null'

        return await query.ToListAsync();
    }

    private async Task<List<ReplayPlayerReviewDto>> GetDsstatsPlayerRatingChartData(PlayerId playerId,
                                                           int year,
                                                           CancellationToken token)
    {
        (var fromDate, var toDate) = GetFromTo(year);

        var query = from p in context.Players
                    from rp in p.ReplayPlayers
                    join r in context.Replays on rp.ReplayId equals r.ReplayId
                    join rr in context.ReplayRatings on r.ReplayId equals rr.ReplayId
                    join rpr in context.RepPlayerRatings on rp.ReplayPlayerId equals rpr.ReplayPlayerId
                    where r.GameTime >= fromDate
                     && r.GameTime < toDate
                     && p.ToonId == playerId.ToonId
                     && p.RealmId == playerId.RealmId
                     && p.RegionId == playerId.RegionId
                    group new { r, rr, rpr } by new { r.GameTime.Year, r.GameTime.Month, r.GameTime.Day, rr.RatingType } into g
                    select new ReplayPlayerReviewDto()
                    {
                        Replay = new(g.Key.Year, g.Key.Month, g.Key.Day)
                        {
                            RatingType = g.Key.RatingType,
                        },
                        ReplayPlayerRatingInfo = new()
                        {
                            Rating = Math.Round(g.Max(a => a.rpr.Rating), 2),
                            Games = g.Max(m => m.rpr.Games)
                        }
                    };
        return await query.ToListAsync(token);
    }

    private (DateTime, DateTime) GetFromTo(int year)
    {
        var from = new DateTime(year, 1, 1);
        var to = new DateTime(year + 1, 1, 1);
        return (from, to);
    }
}


