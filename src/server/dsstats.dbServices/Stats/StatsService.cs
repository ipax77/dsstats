using dsstats.shared;
using dsstats.shared.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace dsstats.db.Services.Stats;

public interface IStatsProvider
{
    StatsType StatsType { get; }
    Task<IStatsResponse> GetStatsUntypedAsync(StatsRequest request, CancellationToken token = default);
}

public interface IStatsProvider<T> : IStatsProvider where T : IStatsResponse
{
    Task<T> GetStatsAsync(StatsRequest request, CancellationToken token = default);
}

public abstract class StatsProviderBase<T> : IStatsProvider<T> where T : IStatsResponse
{
    public abstract StatsType StatsType { get; }
    public abstract Task<T> GetStatsAsync(StatsRequest request, CancellationToken token = default);

    async Task<IStatsResponse> IStatsProvider.GetStatsUntypedAsync(StatsRequest request, CancellationToken token)
        => await GetStatsAsync(request, token);
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
        var timeInfo = Data.GetTimePeriodInfo(request.TimePeriod);
        var noEnd = timeInfo.End > DateTime.Today.AddDays(-2);

        var query = from r in context.Replays
                    from rp in r.Players
                    from rr in r.Ratings
                    join rpr in context.ReplayPlayerRatings
                        on new { rp.ReplayPlayerId, rr.ReplayRatingId }
                        equals new { rpr.ReplayPlayerId, rpr.ReplayRatingId }
                    where r.Gametime >= timeInfo.Start
                        && (noEnd || r.Gametime < timeInfo.End)
                        && rr.RatingType == request.RatingType
                        && (request.WithLeavers || rr.LeaverType == LeaverType.None)
                        && rp.Race != Commander.None
                    group new { rp, rr, rpr, r } by rp.Race into g
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

    private async Task<List<WinrateEnt>> GetWinrateInterestData(StatsRequest request, CancellationToken token)
    {
        var timeInfo = Data.GetTimePeriodInfo(request.TimePeriod);
        var noEnd = timeInfo.End > DateTime.Today.AddDays(-2);

        var query = from r in context.Replays
                    from rp in r.Players
                    from rr in r.Ratings
                    join rpr in context.ReplayPlayerRatings
                        on new { rp.ReplayPlayerId, rr.ReplayRatingId }
                        equals new { rpr.ReplayPlayerId, rpr.ReplayRatingId }
                    where r.Gametime >= timeInfo.Start
                        && (noEnd || r.Gametime < timeInfo.End)
                        && rr.RatingType == request.RatingType
                        && (request.WithLeavers || rr.LeaverType == LeaverType.None)
                        && rp.Race == request.Interest
                        && rp.OppRace != Commander.None
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

    private async Task<T> GetOrSetCachedAsync<T>(string key, Func<Task<T>> factory, TimeSpan duration)
    {
        if (memoryCache.TryGetValue(key, out var cached) && cached is T value)
            return value;

        value = await factory();
        memoryCache.Set(key, value, duration);
        return value;
    }
}

public static class StatsRequestExtensions
{
    public static string GetMemKey(this StatsRequest request, StatsType statsType)
    {
        return $"{statsType}:{request.TimePeriod}|{request.RatingType}|{request.Interest}|{request.WithLeavers}";
    }
}



