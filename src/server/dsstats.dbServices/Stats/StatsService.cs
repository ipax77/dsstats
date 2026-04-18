using dsstats.shared;
using dsstats.shared.Interfaces;

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




