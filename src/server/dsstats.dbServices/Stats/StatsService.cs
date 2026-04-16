using dsstats.db;
using dsstats.shared;
using dsstats.shared.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace dsstats.dbServices.Stats;

public interface IStatsProvider
{
    StatsType StatsType { get; }
    Task<IStatsResponse> GetStatsUntypedAsync(StatsRequest request, CancellationToken token = default);
    Task<IStatsResponse> GetUserStatsUntypedAsync(StatsRequest request, ToonIdDto toonId, CancellationToken token = default);
}

public interface IStatsProvider<T> : IStatsProvider where T : IStatsResponse
{
    Task<T> GetStatsAsync(StatsRequest request, CancellationToken token = default);
    Task<T> GetUserStatsAsync(StatsRequest request, ToonIdDto toonId, CancellationToken token = default);
}

public abstract class StatsProviderBase<T> : IStatsProvider<T> where T : IStatsResponse
{
    public abstract StatsType StatsType { get; }
    public abstract Task<T> GetStatsAsync(StatsRequest request, CancellationToken token = default);
    public abstract Task<T> GetUserStatsAsync(StatsRequest request, ToonIdDto toonId, CancellationToken token = default);

    async Task<IStatsResponse> IStatsProvider.GetStatsUntypedAsync(StatsRequest request, CancellationToken token)
        => await GetStatsAsync(request, token);
    async Task<IStatsResponse> IStatsProvider.GetUserStatsUntypedAsync(StatsRequest request, ToonIdDto toonId, CancellationToken token)
        => await GetUserStatsAsync(request, toonId, token);
}

public sealed class StatsService(IEnumerable<IStatsProvider> providers) : IStatsService
{
    private readonly Dictionary<StatsType, IStatsProvider> providers = providers.ToDictionary(p => p.StatsType, p => p);

    public async Task<T> GetStatsAsync<T>(StatsType type, StatsRequest request, CancellationToken token = default)
        where T : IStatsResponse
    {
        if (!providers.TryGetValue(type, out var provider))
            throw new InvalidOperationException($"No provider registered for type '{type}'.");

        var result = await provider.GetStatsUntypedAsync(request, token);

        if (result is not T typed)
            throw new InvalidCastException($"Provider for {type} did not return {typeof(T).Name}.");

        return typed;
    }

    public async Task<T> GetUserStatsAsync<T>(StatsType type, StatsRequest request, ToonIdDto toonId, CancellationToken token = default)
    {
        if (!providers.TryGetValue(type, out var provider))
            throw new InvalidOperationException($"No provider registered for type '{type}'.");

        var result = await provider.GetUserStatsUntypedAsync(request, toonId, token);

        if (result is not T typed)
            throw new InvalidCastException($"Provider for {type} did not return {typeof(T).Name}.");

        return typed;
    }
}

public class WinrateStatsProvider(DsstatsContext context, IMemoryCache memoryCache) : StatsProviderBase<WinrateResponse>
{
    public override StatsType StatsType => StatsType.Winrate;

    public override async Task<WinrateResponse> GetStatsAsync(StatsRequest request, CancellationToken token = default)
    {
        var memKey = request.GetMemKey(StatsType);

        return await memoryCache.GetOrCreateAsync(memKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(3);

            var winrateData = request.Interest == Commander.None
                ? await GetWinrateBaseData(request, token)
                : await GetWinrateInterestData(request, token);

            return new WinrateResponse { WinrateEnts = winrateData };
        }) ?? new();
    }

    private async Task<List<WinrateEnt>> GetWinrateBaseData(StatsRequest request, CancellationToken token)
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
        var f = StatsFilterResolver.Resolve(request);

        var playerId = await context.Players.Where(f => f.ToonId.Id == toonId.Id
            && f.ToonId.Region == toonId.Region
            && f.ToonId.Realm == toonId.Realm)
            .Select(s => s.PlayerId)
            .FirstOrDefaultAsync();

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

public static class StatsRequestExtensions
{
    public static string GetMemKey(this StatsRequest request, StatsType statsType)
        => GetMemKey(request, statsType, includeInterest: true);

    public static string GetMemKeyWithoutInterest(this StatsRequest request, StatsType statsType)
        => GetMemKey(request, statsType, includeInterest: false);

    private static string GetMemKey(this StatsRequest request, StatsType statsType, bool includeInterest)
    {
        var interestPart = includeInterest ? request.Interest.ToString() : "all";

        if (request.Filter is null)
        {
            return $"{statsType}:{request.TimePeriod}|{request.RatingType}|{interestPart}|{request.WithLeavers}";
        }

        return $"{statsType}:{request.RatingType}|{interestPart}|{request.WithLeavers}"
            + $"{request.Filter.DateRange.From:yyyy-MM-dd}|{request.Filter.DateRange.To:yyyy-MM-dd}"
            + $"{request.Filter.RatingRange.From}|{request.Filter.RatingRange.To}"
            + $"{request.Filter.DurationRange.From}|{request.Filter.DurationRange.To}"
            + $"{request.Filter.Exp2WinRange.From}|{request.Filter.Exp2WinRange.To}"
            + $"{request.Filter.TeamRatingRange.From}|{request.Filter.TeamRatingRange.To}";
    }
}




