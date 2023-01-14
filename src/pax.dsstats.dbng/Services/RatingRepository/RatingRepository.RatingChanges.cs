using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using pax.dsstats.dbng.Extensions;
using pax.dsstats.shared;

namespace pax.dsstats.dbng.Services;

public partial class RatingRepository
{
    public async Task<int> GetRatingChangesCount(RatingChangesRequest request, CancellationToken token)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var ratingStats = await GetRatingStatsQueriable(context, request);
        ratingStats = FilterPlayerRatingStats(ratingStats, request);

        return ratingStats.Count();
    }

    public async Task<RatingChangesResult> GetRatingChanges(RatingChangesRequest request, CancellationToken token)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var ratingStats = await GetRatingStatsQueriable(context, request);
        ratingStats = FilterPlayerRatingStats(ratingStats, request);
        ratingStats = OrderPlayerRatingStats(ratingStats, request);

        var stats = ratingStats
                .Skip(request.Skip)
                .Take(request.Take)
                .ToList();

        return new()
        {
            PlayerRatingStats = stats,
        };
    }

    private static IQueryable<PlayerRatingStat> FilterPlayerRatingStats(IQueryable<PlayerRatingStat> stats, RatingChangesRequest request)
    {
        if (String.IsNullOrEmpty(request.Search))
        {
            return stats;
        }
        else
        {
            return stats.Where(x => x.RequestNames.Name.ToUpper() == request.Search.ToUpper());
        }
    }

    private static IQueryable<PlayerRatingStat> OrderPlayerRatingStats(IQueryable<PlayerRatingStat> stats, RatingChangesRequest request)
    {
        foreach (var order in request.Orders)
        {
            if (order.Ascending)
            {
                stats = stats.AppendOrderBy(order.Property);
            }
            else
            {
                stats = stats.AppendOrderByDescending(order.Property);
            }
        }
        return stats;
    }

    private async Task<IQueryable<PlayerRatingStat>> GetRatingStatsQueriable(ReplayContext context, RatingChangesRequest request)
    {
        var fromDate = GetRatingChangesFromDate(request.TimePeriod);

        var memKey = $"ratingChange{fromDate.ToString(@"yyyyMMdd")}{request.RatingType}{request.TimePeriod}";

        using var scope = scopeFactory.CreateScope();
        var memoryCache = scope.ServiceProvider.GetRequiredService<IMemoryCache>();

        if (!memoryCache.TryGetValue(memKey, out List<PlayerRatingStat> stats))
        {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            var statsQuery = from r in context.Replays
                   from rpr in r.ReplayRatingInfo.RepPlayerRatings
                   where r.GameTime > fromDate
                     && rpr.ReplayPlayer.Player.UploaderId != null
                     && r.ReplayRatingInfo.RatingType == request.RatingType
                   group new { rpr.ReplayPlayer.Player, rpr } by new
                   {
                       rpr.ReplayPlayer.Player.ToonId,
                       rpr.ReplayPlayer.Player.Name,
                       rpr.ReplayPlayer.Player.RegionId
                   } into g
                   where g.Count() > GetRatingChangeLimit(request.TimePeriod)
                   select new PlayerRatingStat
                   {
                       RequestNames = new()
                       {
                           Name = g.Key.Name,
                           ToonId = g.Key.ToonId,
                           RegionId = g.Key.RegionId,
                       },
                       Games = g.Count(),
                       RatingChange = MathF.Round(g.Sum(s => s.rpr.RatingChange), 2)
                   };
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            stats = await statsQuery.ToListAsync();
            memoryCache.Set(memKey, stats, TimeSpan.FromDays(1));
        }
        return stats.AsQueryable();
    }

    private static int GetRatingChangeLimit(RatingChangeTimePeriod timePeriod)
    {
        return timePeriod switch
        {
            RatingChangeTimePeriod.Past24h => 2,
            RatingChangeTimePeriod.Past10Days => 5,
            _ => 10
        };
    }

    public static DateTime GetRatingChangesFromDate(RatingChangeTimePeriod timePeriod)
    {
        DateTime now = DateTime.UtcNow;
        DateTime today = new DateTime(now.Year, now.Month, now.Day);
        return timePeriod switch
        {
            RatingChangeTimePeriod.Past24h => now.AddHours(-24),
            RatingChangeTimePeriod.Past10Days => today.AddDays(-10),
            _ => today.AddDays(-30)
        };
    }
}


