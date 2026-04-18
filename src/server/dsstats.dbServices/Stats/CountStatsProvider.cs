using dsstats.db;
using dsstats.shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace dsstats.dbServices.Stats;

public class CountStatsProvider(DsstatsContext context, IMemoryCache memoryCache) : StatsProviderBase<CountResponse>
{
    public override StatsType StatsType => StatsType.Count;

    public override async Task<CountResponse> GetStatsAsync(StatsRequest request, CancellationToken token = default)
    {
        var memKey = request.GetMemKey(StatsType);

        return await memoryCache.GetOrCreateAsync(memKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(3);

            var countEnts = await GetCountBaseData(request, token);
            var countStats = await GetCountStats(request, token);

            return new CountResponse() 
            { 
                Count = countStats.Count,
                ReplaysWithLeaver = countStats.ReplaysWithLeaver,
                ReplaysWithoutRating = countStats.ReplaysWithoutRating,
                NoResult = countStats.NoResult,
                Under5Min = countStats.Under5Min,
                CountEnts = countEnts
            };
        }) ?? new();
    }

    private async Task<List<CountEnt>> GetCountBaseData(StatsRequest request, CancellationToken token)
    {
        var f = StatsFilterResolver.Resolve(request);

        var query =
            from r in context.Replays
            from rp in r.Players
            join rr in context.Set<ReplayRating>()
                on r.ReplayId equals rr.ReplayId into rrGrouping
            from grr in rrGrouping.DefaultIfEmpty()
            join rpr in context.Set<ReplayPlayerRating>()
                on rp.ReplayPlayerId equals rpr.ReplayPlayerId into rprGrouping
            from grpr in rprGrouping.DefaultIfEmpty()
            where
                r.Gametime >= f.FromDate &&
                (!f.HasToDate || r.Gametime < f.ToDate) &&
                (grr == null || grr.RatingType == request.RatingType) &&
                (request.WithLeavers || grr == null || grr.LeaverType == LeaverType.None) &&
                (f.RatingFrom == null || grpr.RatingBefore >= f.RatingFrom) &&
                (f.RatingTo == null || grpr.RatingBefore <= f.RatingTo) &&
                (f.DurationFrom == null || r.Duration >= f.DurationFrom) &&
                (f.DurationTo == null || r.Duration <= f.DurationTo) &&
                (f.Exp2WinFrom == null || grr.ExpectedWinProbability >= f.Exp2WinFrom) &&
                (f.Exp2WinTo == null || grr.ExpectedWinProbability <= f.Exp2WinTo) &&
                (f.TeamRatingTo == null || grr.AvgRating <= f.TeamRatingTo) &&
                (f.TeamRatingFrom == null || grr.AvgRating >= f.TeamRatingFrom) &&
                (request.Interest == Commander.None || rp.Race == request.Interest)
            group new { r, rp } by rp.Race into g
            select new CountEnt()
            {
                Commander = g.Key,
                Count = g.Count(),
            };

        var ents = await query.ToListAsync(token);
        return ents.OrderByDescending(o => o.Count).ToList();
    }

    private async Task<CountStats> GetCountStats(StatsRequest request, CancellationToken token)
    {
        var f = StatsFilterResolver.Resolve(request);

        var baseQuery =
            from r in context.Replays
            join rr in context.Set<ReplayRating>()
                on r.ReplayId equals rr.ReplayId into rrGrouping
            from grr in rrGrouping.DefaultIfEmpty()
            where
                r.Gametime >= f.FromDate &&
                (!f.HasToDate || r.Gametime < f.ToDate) &&
                (grr == null || grr.RatingType == request.RatingType) &&
                (f.DurationFrom == null || r.Duration >= f.DurationFrom) &&
                (f.DurationTo == null || r.Duration <= f.DurationTo) &&
                (f.Exp2WinFrom == null || grr.ExpectedWinProbability >= f.Exp2WinFrom) &&
                (f.Exp2WinTo == null || grr.ExpectedWinProbability <= f.Exp2WinTo) &&
                (f.TeamRatingTo == null || grr.AvgRating <= f.TeamRatingTo) &&
                (f.TeamRatingFrom == null || grr.AvgRating >= f.TeamRatingFrom)
            select new
            {
                r.ReplayId,
                HasLeaver = grr != null && grr.LeaverType != LeaverType.None,
                HasRating = grr != null,
                NoResult = r.WinnerTeam == 0,
                Under5Min = r.Duration < 300
            };

        var stats = await baseQuery.ToListAsync(token);

        var count = stats.Select(s => s.ReplayId).Distinct().Count();
        var replaysWithLeaver = stats.Where(x => x.HasLeaver).Select(s => s.ReplayId).Distinct().Count();
        var replaysWithoutRating = stats.Where(x => !x.HasRating).Select(s => s.ReplayId).Distinct().Count();
        var noResult = stats.Where(x => x.NoResult).Select(s => s.ReplayId).Distinct().Count();
        var under5min = stats.Where(x => x.Under5Min).Select(s => s.ReplayId).Distinct().Count();
        return new(count, replaysWithLeaver, replaysWithoutRating, noResult, under5min);
    }

    public override async Task<CountResponse> GetUserStatsAsync(StatsRequest request, ToonIdDto toonId, CancellationToken token = default)
    {
        var memKey = request.GetMemKey(StatsType) + $"|{toonId.Id}|{toonId.Realm}|{toonId.Region}";

        return await memoryCache.GetOrCreateAsync(memKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(3);

            var countEnts = await ProduceUserStatsAsync(request, toonId, token);

            return new CountResponse { CountEnts = countEnts };
        }) ?? new();
    }

    private async Task<List<CountEnt>> ProduceUserStatsAsync(StatsRequest request, ToonIdDto toonId, CancellationToken token = default)
    {
        var f = StatsFilterResolver.Resolve(request);

        return [];
    }
}


internal sealed record CountStats(int Count, int ReplaysWithLeaver, int ReplaysWithoutRating, int NoResult, int Under5Min);

