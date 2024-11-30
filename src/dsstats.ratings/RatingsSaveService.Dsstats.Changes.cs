using dsstats.db8;
using dsstats.shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace dsstats.ratings;

public partial class RatingsSaveService
{
    private async Task SetDsstatsRatingChange(ReplayContext context, string connectionString)
    {
        Stopwatch sw = Stopwatch.StartNew();

        await ClearDsstatsRatingChanges(connectionString);

        await SetDsstatsRatingChange(context, RatingType.Cmdr);
        await SetDsstatsRatingChange(context, RatingType.Std);
        await SetDsstatsRatingChange(context, RatingType.CmdrTE);
        await SetDsstatsRatingChange(context, RatingType.StdTE);

        sw.Stop();
        logger.LogInformation("dsstats player rating changes set in {ms}", sw.ElapsedMilliseconds);
    }

    private async Task SetDsstatsRatingChange(ReplayContext context, RatingType ratingType)
    {
        var fromDate = DateTime.Today.AddDays(-30);
        DateTime bp24h = DateTime.UtcNow.AddDays(-1);
        DateTime bp10d = DateTime.Today.AddDays(-10);

        var query = from p in context.Players
                    from rp in p.ReplayPlayers
                    join pr in context.PlayerRatings on p.PlayerId equals pr.PlayerId
                    join rpr in context.RepPlayerRatings on rp.ReplayPlayerId equals rpr.ReplayPlayerId
                    join rr in context.ReplayRatings on rp.ReplayId equals rr.ReplayId
                    join r in context.Replays on rp.ReplayId equals r.ReplayId
                    where r.GameTime > fromDate && pr.RatingType == ratingType && rr.RatingType == ratingType
                    select new
                    {
                        PlayerId = new PlayerId(p.ToonId, p.RegionId, p.RealmId),
                        Id = pr.PlayerRatingId,
                        GameTime = r.GameTime,
                        Change = rpr.RatingChange
                    };

        Dictionary<PlayerId, PlayerRatingChange> changes = new();

        var data = await query.ToListAsync();

        foreach (var info in data)
        {
            if (!changes.TryGetValue(info.PlayerId, out PlayerRatingChange? ratingChange)
                || ratingChange is null)
            {
                ratingChange = changes[info.PlayerId] = new() { PlayerRatingId = info.Id };
            }

            if (info.GameTime > bp24h)
            {
                ratingChange.Change24h += info.Change;
            }

            if (info.GameTime > bp10d)
            {
                ratingChange.Change10d += info.Change;
            }

            if (info.GameTime > fromDate)
            {
                ratingChange.Change30d += info.Change;
            }
        }

        context.PlayerRatingChanges.AddRange(changes.Values);
        await context.SaveChangesAsync();
    }
}
