using dsstats.db8;
using dsstats.shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace dsstats.ratings;

public partial class RatingsSaveService
{
    private async Task SetArcadeRatingChange(ReplayContext context, string connectionString)
    {
        Stopwatch sw = Stopwatch.StartNew();

        await ClearArcadeRatingChanges(connectionString);

        await SetArcadeRatingChange(context, RatingType.Cmdr);
        await SetArcadeRatingChange(context, RatingType.Std);

        sw.Stop();
        logger.LogWarning("arcade player rating changes set in {ms}", sw.ElapsedMilliseconds);
    }

    private async Task SetArcadeRatingChange(ReplayContext context, RatingType ratingType)
    {
        var fromDate = DateTime.Today.AddDays(-30);
        DateTime bp24h = DateTime.UtcNow.AddDays(-1);
        DateTime bp10d = DateTime.Today.AddDays(-10);

        var query = from p in context.ArcadePlayers
                    from rp in p.ArcadeReplayPlayers
                    join pr in context.ArcadePlayerRatings on p.ArcadePlayerId equals pr.ArcadePlayerId
                    join rpr in context.ArcadeReplayPlayerRatings on rp.ArcadeReplayPlayerId equals rpr.ArcadeReplayPlayerId
                    join rr in context.ArcadeReplayRatings on rp.ArcadeReplayId equals rr.ArcadeReplayId
                    join r in context.ArcadeReplays on rp.ArcadeReplayId equals r.ArcadeReplayId
                    where r.CreatedAt > fromDate && pr.RatingType == ratingType && rr.RatingType == ratingType
                    select new
                    {
                        PlayerId = new PlayerId(p.ProfileId, p.RegionId, p.RealmId),
                        Id = pr.ArcadePlayerRatingId,
                        GameTime = r.CreatedAt,
                        Change = rpr.RatingChange
                    };

        Dictionary<PlayerId, ArcadePlayerRatingChange> changes = new();

        var data = await query.ToListAsync();

        foreach (var info in data)
        {
            if (!changes.TryGetValue(info.PlayerId, out ArcadePlayerRatingChange? ratingChange)
                || ratingChange is null)
            {
                ratingChange = changes[info.PlayerId] = new() { ArcadePlayerRatingId = info.Id };
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

        context.ArcadePlayerRatingChanges.AddRange(changes.Values);
        await context.SaveChangesAsync();
    }
}
