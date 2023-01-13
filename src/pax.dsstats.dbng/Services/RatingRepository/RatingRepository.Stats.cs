using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace pax.dsstats.dbng.Services;

public partial class RatingRepository
{
    public async Task GetRatingStats()
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

#pragma warning disable CS8602 // Dereference of a possibly null reference.
        var bab = from r in context.Replays
                  from rpr in r.ReplayRatingInfo.RepPlayerRatings
                  where r.GameTime > new DateTime(2023, 1, 1)
                    && rpr.ReplayPlayer.Player.UploaderId != null
                    && r.ReplayRatingInfo.RatingType == shared.RatingType.Cmdr
                  group new { rpr.ReplayPlayer.Player, rpr } by rpr.ReplayPlayer.Player.ToonId into g
                  where g.Count() > 10
                  select new
                  {
                      ToonId = g.Key,
                      Count = g.Count(),
                      RatingChange = g.Sum(s => s.rpr.RatingChange)
                  };
#pragma warning restore CS8602 // Dereference of a possibly null reference.

        var lbab = await bab
            .OrderByDescending(o => o.RatingChange)
            .Take(5)
            .ToListAsync();

        Console.WriteLine(lbab.FirstOrDefault());
    }
}
