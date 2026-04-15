using dsstats.db;
using dsstats.shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace dsstats.dbServices.Stats;

public sealed class SynergyStatsProvider(DsstatsContext context, IMemoryCache memoryCache) : StatsProviderBase<SynergyResponse>
{
    public override StatsType StatsType => StatsType.Synergy;

    public override async Task<SynergyResponse> GetStatsAsync(StatsRequest request, CancellationToken token = default)
    {
        var memKey = request.GetMemKeyWithoutInterest(StatsType);

        return await memoryCache.GetOrCreateAsync(memKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(3);

            var ents = await GetSynergyDataAsync(request, token);
            return new SynergyResponse { SynergyEnts = ents };
        }) ?? new();
    }

    private async Task<List<SynergyEnt>> GetSynergyDataAsync(StatsRequest request, CancellationToken token)
    {
        var f = StatsFilterResolver.Resolve(request);

        var query =
            from r in context.Replays
            from rp in r.Players
            from teammate in r.Players
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
                teammate.TeamId == rp.TeamId &&
                teammate.ReplayPlayerId < rp.ReplayPlayerId &&
                teammate.ReplayPlayerId != rp.ReplayPlayerId &&
                (int)rp.Race > 3 &&
                (int)teammate.Race > 3
            group new { rp, teammate, rpr } by new
            {
                Commander = (int)rp.Race < (int)teammate.Race ? rp.Race : teammate.Race,
                Teammate = (int)rp.Race < (int)teammate.Race ? teammate.Race : rp.Race
            } into g
            select new SynergyEnt
            {
                Commander = g.Key.Commander,
                Teammate = g.Key.Teammate,
                Games = g.Count(),
                Wins = g.Sum(x => x.rp.Result == PlayerResult.Win ? 1 : 0),
                Winrate = g.Sum(x => x.rp.Result == PlayerResult.Win ? 1.0 : 0.0) / g.Count(),
                AvgGain = Math.Round(g.Average(x => x.rpr.RatingDelta), 2)
            };

        return await query
            .OrderBy(x => x.Commander)
            .ThenBy(x => x.Teammate)
            .ToListAsync(token);
    }
}
