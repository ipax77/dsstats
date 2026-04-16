using dsstats.db;
using dsstats.shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace dsstats.dbServices.Stats;

public sealed class TimelineStatsProvider(DsstatsContext context, IMemoryCache memoryCache) : StatsProviderBase<TimelineResponse>
{
    public override StatsType StatsType => StatsType.Timeline;

    public override async Task<TimelineResponse> GetStatsAsync(StatsRequest request, CancellationToken token = default)
    {
        var memKey = request.GetMemKeyWithoutInterest(StatsType);

        return await memoryCache.GetOrCreateAsync(memKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(3);

            var ents = await GetTimelineDataAsync(request, token);
            return new TimelineResponse { TimelineEnts = ents };
        }) ?? new();
    }

    public override Task<TimelineResponse> GetUserStatsAsync(StatsRequest request, ToonIdDto toonId, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    private async Task<List<TimelineEnt>> GetTimelineDataAsync(StatsRequest request, CancellationToken token)
    {
        var f = StatsFilterResolver.Resolve(request);

        var query =
            from r in context.Replays
            from rp in r.Players
            from rr in r.Ratings
            join rpr in context.ReplayPlayerRatings
                on new { rp.ReplayPlayerId, rr.ReplayRatingId }
                equals new { rpr.ReplayPlayerId, rpr.ReplayRatingId }

            where
                r.Gametime >= f.FromDate &&
                (!f.HasToDate || r.Gametime < f.ToDate) &&
                r.GameMode == GameMode.Commanders &&
                r.PlayerCount == 6 &&
                rr.RatingType == request.RatingType &&
                (request.WithLeavers || rr.LeaverType == LeaverType.None) &&
                (f.RatingFrom == null || rpr.RatingBefore >= f.RatingFrom) &&
                (f.RatingTo == null || rpr.RatingBefore <= f.RatingTo) &&
                (f.DurationFrom == null || r.Duration >= f.DurationFrom) &&
                (f.DurationTo == null || r.Duration <= f.DurationTo) &&
                (f.Exp2WinFrom == null || rr.ExpectedWinProbability >= f.Exp2WinFrom) &&
                (f.Exp2WinTo == null || rr.ExpectedWinProbability <= f.Exp2WinTo) &&
                (f.TeamRatingTo == null || rr.AvgRating <= f.TeamRatingTo) &&
                (f.TeamRatingFrom == null || rr.AvgRating >= f.TeamRatingFrom) &&
                (int)rp.Race > 3 &&
                r.Duration >= 5 * 60

            let bucket =
                r.Duration >= 35 * 60
                    ? -1
                    : (int)Math.Floor(((double)r.Duration / 60.0 - 5.0) / 3.0)

            group new { rp, rpr, r } by new
            {
                Commander = rp.Race,
                Bucket = bucket
            }
            into g

            select new TimelineDbEnt(
                g.Key.Commander,

                // Bucket label or start (depending on your DTO)
                g.Key.Bucket == -1
                    ? 35
                    : g.Key.Bucket * 3 + 5,

                g.Count(),

                g.Sum(x => x.rp.Result == PlayerResult.Win ? 1 : 0),

                Math.Round(g.Average(x => x.rpr.RatingDelta), 2)
            );

        var dbEnts = await query.ToListAsync(token);

        var timelineEnts = dbEnts
            .GroupBy(e => e.Commander)
            .Select(g => new TimelineEnt
            {
                Commander = g.Key,
                Steps = g.Select(e => new TimelineStep
                {
                    BucketStart = e.BucketStart,
                    Count = e.Count,
                    Wins = e.Wins,
                    AvgGain = e.AvgGain
                }).OrderBy(s => s.BucketStart).ToList()
            })
            .ToList();

        return timelineEnts;
    }
}

internal sealed record TimelineDbEnt(Commander Commander, int BucketStart, int Count, int Wins, double AvgGain);
