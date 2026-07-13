using dsstats.db;
using dsstats.shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace dsstats.dbServices.Stats;

public class WinrateStatsProvider(IDbContextFactory<DsstatsContext> contextFactory, IMemoryCache memoryCache) : StatsProviderBase<WinrateResponse>
{
    public override StatsType StatsType => StatsType.Winrate;

    public override async Task<WinrateResponse> GetStatsAsync(StatsRequest request, CancellationToken token = default)
    {
        var memKey = request.GetMemKey(StatsType);

        return await memoryCache.GetOrCreateAsync(memKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(3);

            if (request.Comparison is not null)
            {
                var comparisonData = await GetComparisonData(request, token);
                return new WinrateResponse { ComparisonEnts = comparisonData };
            }

            var winrateData = request.Interest == Commander.None
                ? await GetWinrateBaseData(request, token)
                : await GetWinrateInterestData(request, token);

            return new WinrateResponse { WinrateEnts = winrateData };
        }) ?? new();
    }

    private async Task<List<WinrateComparisonEnt>> GetComparisonData(StatsRequest request, CancellationToken token)
    {
        var comparison = request.Comparison
            ?? throw new InvalidOperationException("A comparison request is required.");
        var windows = comparison.Resolve(DateTime.UtcNow.Date);

        await using var context = await contextFactory.CreateDbContextAsync(token);
        var f = StatsFilterResolver.Resolve(request);

        var aggregates = request.Interest == Commander.None
            ? await GetBaseComparisonAggregates(context, request, f, windows, token)
            : await GetInterestComparisonAggregates(context, request, f, windows, token);

        var byCommander = aggregates
            .GroupBy(x => x.Commander)
            .OrderBy(group => group.Key);
        List<WinrateComparisonEnt> result = new(aggregates.Count);

        foreach (var group in byCommander)
        {
            var beforeAggregate = group.FirstOrDefault(x => !x.IsAfter);
            var afterAggregate = group.FirstOrDefault(x => x.IsAfter);
            var before = ToPeriod(beforeAggregate);
            var after = ToPeriod(afterAggregate);
            var confidence = WinrateComparisonStatistics.CalculateWelch95(
                ToMoments(beforeAggregate),
                ToMoments(afterAggregate));

            result.Add(new WinrateComparisonEnt
            {
                Commander = group.Key,
                Before = before,
                After = after,
                AverageGainDifference = Math.Round(confidence.Difference, 2),
                RawWinrateDifference = after.RawWinrate - before.RawWinrate,
                AverageGainConfidenceLow = confidence.Low is null ? null : Math.Round(confidence.Low.Value, 2),
                AverageGainConfidenceHigh = confidence.High is null ? null : Math.Round(confidence.High.Value, 2),
                ConfidenceStatus = confidence.Status
            });
        }

        return result
            .OrderByDescending(x => x.AverageGainDifference)
            .ToList();
    }

    private static async Task<List<ComparisonAggregate>> GetBaseComparisonAggregates(
        DsstatsContext context,
        StatsRequest request,
        ResolvedStatsFilter f,
        WinrateComparisonWindows windows,
        CancellationToken token)
    {
        var replayCommanderStats =
            from r in context.Replays
            from rp in r.Players
            from rr in r.Ratings
            join rpr in context.ReplayPlayerRatings
                on new { rp.ReplayPlayerId, rr.ReplayRatingId }
                equals new { rpr.ReplayPlayerId, rpr.ReplayRatingId }
            where
                r.Gametime >= windows.BeforeFrom &&
                r.Gametime < windows.AfterToExclusive &&
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
                rp.Race != Commander.None
            group new { rp, rpr } by new
            {
                Commander = rp.Race,
                r.ReplayId,
                IsAfter = r.Gametime >= windows.AfterFrom
            }
            into replayGroup
            select new
            {
                replayGroup.Key.Commander,
                replayGroup.Key.IsAfter,
                Appearances = replayGroup.Count(),
                Wins = replayGroup.Sum(x => x.rp.Result == PlayerResult.Win ? 1 : 0),
                AverageGain = replayGroup.Average(x => x.rpr.RatingDelta)
            };

        return await (
            from replayStat in replayCommanderStats
            group replayStat by new { replayStat.Commander, replayStat.IsAfter }
            into periodGroup
            select new ComparisonAggregate
            {
                Commander = periodGroup.Key.Commander,
                IsAfter = periodGroup.Key.IsAfter,
                Appearances = periodGroup.Sum(x => x.Appearances),
                Replays = periodGroup.Count(),
                Wins = periodGroup.Sum(x => x.Wins),
                GainSum = periodGroup.Sum(x => x.AverageGain),
                GainSumSquares = periodGroup.Sum(x => x.AverageGain * x.AverageGain)
            }).ToListAsync(token);
    }

    private static async Task<List<ComparisonAggregate>> GetInterestComparisonAggregates(
        DsstatsContext context,
        StatsRequest request,
        ResolvedStatsFilter f,
        WinrateComparisonWindows windows,
        CancellationToken token)
    {
        var replayCommanderStats =
            from r in context.Replays
            from rp in r.Players
            from rr in r.Ratings
            join rpr in context.ReplayPlayerRatings
                on new { rp.ReplayPlayerId, rr.ReplayRatingId }
                equals new { rpr.ReplayPlayerId, rpr.ReplayRatingId }
            where
                r.Gametime >= windows.BeforeFrom &&
                r.Gametime < windows.AfterToExclusive &&
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
                rp.Race == request.Interest &&
                rp.OppRace != Commander.None
            group new { rp, rpr } by new
            {
                Commander = rp.OppRace,
                r.ReplayId,
                IsAfter = r.Gametime >= windows.AfterFrom
            }
            into replayGroup
            select new
            {
                replayGroup.Key.Commander,
                replayGroup.Key.IsAfter,
                Appearances = replayGroup.Count(),
                Wins = replayGroup.Sum(x => x.rp.Result == PlayerResult.Win ? 1 : 0),
                AverageGain = replayGroup.Average(x => x.rpr.RatingDelta)
            };

        return await (
            from replayStat in replayCommanderStats
            group replayStat by new { replayStat.Commander, replayStat.IsAfter }
            into periodGroup
            select new ComparisonAggregate
            {
                Commander = periodGroup.Key.Commander,
                IsAfter = periodGroup.Key.IsAfter,
                Appearances = periodGroup.Sum(x => x.Appearances),
                Replays = periodGroup.Count(),
                Wins = periodGroup.Sum(x => x.Wins),
                GainSum = periodGroup.Sum(x => x.AverageGain),
                GainSumSquares = periodGroup.Sum(x => x.AverageGain * x.AverageGain)
            }).ToListAsync(token);
    }

    private static WinrateComparisonPeriod ToPeriod(ComparisonAggregate? aggregate)
    {
        if (aggregate is null)
        {
            return new();
        }

        return new WinrateComparisonPeriod
        {
            Appearances = aggregate.Appearances,
            Replays = aggregate.Replays,
            Wins = aggregate.Wins,
            AverageRatingGain = aggregate.Replays == 0
                ? 0
                : Math.Round(aggregate.GainSum / aggregate.Replays, 2)
        };
    }

    private static AggregateMoments ToMoments(ComparisonAggregate? aggregate)
    {
        return aggregate is null
            ? new AggregateMoments(0, 0, 0)
            : new AggregateMoments(aggregate.Replays, aggregate.GainSum, aggregate.GainSumSquares);
    }

    private sealed class ComparisonAggregate
    {
        public Commander Commander { get; init; }
        public bool IsAfter { get; init; }
        public int Appearances { get; init; }
        public int Replays { get; init; }
        public int Wins { get; init; }
        public double GainSum { get; init; }
        public double GainSumSquares { get; init; }
    }

    private async Task<List<WinrateEnt>> GetWinrateBaseData(StatsRequest request, CancellationToken token)
    {
        await using var context = await contextFactory.CreateDbContextAsync(token);
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
                rp.Race != Commander.None
            group new { rp, rr, rpr, r } by rp.Race into g
            select new WinrateEnt
            {
                Commander = g.Key,
                Count = g.Count(),
                AvgRating = Math.Round(g.Average(a => a.rpr.RatingBefore), 2),
                AvgPerformance = Math.Round(g.Average(a => a.rpr.RatingDelta), 2),
                Wins = g.Sum(s => s.rp.Result == PlayerResult.Win ? 1 : 0),
                Replays = g.Select(s => s.r.ReplayId).Distinct().Count()
            };

        return await query
            .OrderByDescending(o => o.AvgPerformance)
            .ToListAsync(token);
    }

    private async Task<List<WinrateEnt>> GetWinrateInterestData(StatsRequest request, CancellationToken token)
    {
        await using var context = await contextFactory.CreateDbContextAsync(token);
        var f = StatsFilterResolver.Resolve(request);

        var query = from r in context.Replays
                    from rp in r.Players
                    from rr in r.Ratings
                    join rpr in context.ReplayPlayerRatings
                        on new { rp.ReplayPlayerId, rr.ReplayRatingId }
                        equals new { rpr.ReplayPlayerId, rpr.ReplayRatingId }
                    where
                        r.Gametime >= f.FromDate &&
                        (!f.HasToDate || r.Gametime < f.ToDate) &&
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
                        rp.Race == request.Interest &&
                        rp.OppRace != Commander.None
                    group new { rp, rr, rpr, r } by rp.OppRace into g
                    select new WinrateEnt()
                    {
                        Commander = g.Key,
                        Count = g.Count(),
                        AvgRating = Math.Round(g.Average(a => a.rpr.RatingBefore), 2),
                        AvgPerformance = Math.Round(g.Average(a => a.rpr.RatingDelta), 2),
                        Wins = g.Sum(s => s.rp.Result == PlayerResult.Win ? 1 : 0),
                        Replays = g.Select(s => s.r.ReplayId).Distinct().Count()
                    };
        return await query
            .OrderByDescending(o => o.AvgPerformance)
            .ToListAsync(token);
    }

    public override async Task<WinrateResponse> GetUserStatsAsync(StatsRequest request, ToonIdDto toonId, CancellationToken token = default)
    {
        var memKey = request.GetMemKey(StatsType) + $"|{toonId.Id}|{toonId.Realm}|{toonId.Region}";

        return await memoryCache.GetOrCreateAsync(memKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(3);

            var winrateData = await ProduceUserStatsAsync(request, toonId, token);

            return new WinrateResponse { WinrateEnts = winrateData };
        }) ?? new();
    }

    private async Task<List<WinrateEnt>> ProduceUserStatsAsync(StatsRequest request, ToonIdDto toonId, CancellationToken token = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(token);
        var f = StatsFilterResolver.Resolve(request);

        var playerId = await context.Players.Where(f => f.ToonId.Id == toonId.Id
            && f.ToonId.Region == toonId.Region
            && f.ToonId.Realm == toonId.Realm)
            .Select(s => s.PlayerId)
            .FirstOrDefaultAsync(token);

        if (playerId == 0) return [];

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
                rp.Race != Commander.None &&
                rp.PlayerId == playerId
            group new { rp, rr, rpr, r } by rp.Race into g
            select new WinrateEnt
            {
                Commander = g.Key,
                Count = g.Count(),
                AvgRating = Math.Round(g.Average(a => a.rpr.RatingBefore), 2),
                AvgPerformance = Math.Round(g.Average(a => a.rpr.RatingDelta), 2),
                Wins = g.Sum(s => s.rp.Result == PlayerResult.Win ? 1 : 0),
                Replays = g.Select(s => s.r.ReplayId).Distinct().Count()
            };

        return await query
            .OrderByDescending(o => o.AvgPerformance)
            .ToListAsync(token);
    }
}




