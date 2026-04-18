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

            return new CountResponse { CountEnts = countEnts };
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
                (request.RatingType == RatingType.All || grr.RatingType == request.RatingType) &&
                (request.WithLeavers || grr.LeaverType == LeaverType.None) &&
                (f.RatingFrom == null || grpr.RatingBefore >= f.RatingFrom) &&
                (f.RatingTo == null || grpr.RatingBefore <= f.RatingTo) &&
                (f.DurationFrom == null || r.Duration >= f.DurationFrom) &&
                (f.DurationTo == null || r.Duration <= f.DurationTo) &&
                (f.Exp2WinFrom == null || grr.ExpectedWinProbability >= f.Exp2WinFrom) &&
                (f.Exp2WinTo == null || grr.ExpectedWinProbability <= f.Exp2WinTo) &&
                (f.TeamRatingTo == null || grr.AvgRating <= f.TeamRatingTo) &&
                (f.TeamRatingFrom == null || grr.AvgRating >= f.TeamRatingFrom) &&
                (request.Interest == Commander.None || rp.Race == request.Interest)
            group new { r, rp, grr } by rp.Race into g
            select new CountEnt()
            {
                Commander = g.Key,
                Count = g.Count(),
            };

        var ents = await query.ToListAsync(token);
        return ents.OrderByDescending(o => o.Count).ToList();
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




