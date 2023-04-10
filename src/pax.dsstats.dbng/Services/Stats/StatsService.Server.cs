using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using pax.dsstats.shared;

namespace pax.dsstats.dbng.Services;
public partial class StatsService
{
    public async Task<ServerStatsResponse> GetServerStats(CancellationToken token)
    {
        DateTime now = DateTime.UtcNow;
        DateTime fromDate = new DateTime(now.Year, now.Month, now.Day).AddDays(-90);

        string memKey = $"serverstats{fromDate.ToString(@"yyyyMMdd")}";

        if (!memoryCache.TryGetValue(memKey, out ServerStatsResponse stats))
        {
            stats = new ServerStatsResponse()
            {
                PlayerRatingStats = await GetRatingStats(fromDate, token),
            };
            memoryCache.Set(memKey, stats, TimeSpan.FromHours(24));
        }
        return stats;
    }

    private async Task<Dictionary<RatingType, List<PlayerRatingStat>>> GetRatingStats(DateTime fromDate, CancellationToken token)
    {
#pragma warning disable CS8602 // Dereference of a possibly null reference.

        Dictionary<RatingType, List<PlayerRatingStat>> stats = new();

        foreach (RatingType ratingType in Enum.GetValues(typeof(RatingType)))
        {
            if (ratingType == RatingType.None)
            {
                continue;
            }

            var ratingStats = from r in context.Replays
                              from rpr in r.ReplayRatingInfo.RepPlayerRatings
                              where r.GameTime > fromDate
                                && rpr.ReplayPlayer.Player.UploaderId != null
                                && r.ReplayRatingInfo.RatingType == ratingType
                              group new { rpr.ReplayPlayer.Player, rpr } by new
                              {
                                  rpr.ReplayPlayer.Player.ToonId,
                                  rpr.ReplayPlayer.Player.Name,
                                  rpr.ReplayPlayer.Player.RegionId,
                                  rpr.ReplayPlayer.Player.RealmId
                              } into g
                              where g.Count() > 10
                              select new PlayerRatingStat
                              {
                                  RequestNames = new(g.Key.Name, g.Key.ToonId, g.Key.RegionId, g.Key.ToonId),
                                  Games = g.Count(),
                                  RatingChange = MathF.Round(g.Sum(s => s.rpr.RatingChange), 2)
                              };
            stats[ratingType] = await ratingStats
                .OrderByDescending(o => o.RatingChange)
                .Take(5)
                .ToListAsync(token);
        }
#pragma warning restore CS8602 // Dereference of a possibly null reference.
        return stats;
    }

    private async Task GetMostRatingGainedReplay(DateTime fromDate)
    {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
        var ratingStats = from r in context.Replays
                          where r.GameTime > fromDate
                            && r.ReplayRatingInfo.RatingType == RatingType.Cmdr
                          group r.ReplayRatingInfo by r.ReplayId into g
                          select new
                          {
                              g.Key,
                          };
#pragma warning restore CS8602 // Dereference of a possibly null reference.

        var l = await ratingStats.ToListAsync();
    }
}

