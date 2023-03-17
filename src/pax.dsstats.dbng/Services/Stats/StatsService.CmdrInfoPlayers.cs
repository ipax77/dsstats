
using Microsoft.EntityFrameworkCore;
using pax.dsstats.shared;

namespace pax.dsstats.dbng.Services;

public partial class StatsService
{
    public async Task<List<CmdrPlayerInfo>> GetCmdrPlayerInfos(CmdrInfoRequest request, CancellationToken token = default)
    {
        (var fromDate, var toDate) = Data.TimeperiodSelected(request.TimePeriod);
        if (toDate == DateTime.Today)
        {
            toDate = toDate.AddDays(2);
        }

#pragma warning disable CS8602 // Dereference of a possibly null reference.        
        var group = from pr in context.PlayerRatings
                      from rp in pr.Player.ReplayPlayers
                      where pr.RatingType == request.RatingType
                        && rp.Replay.ReplayRatingInfo.RatingType == request.RatingType
                        && rp.Race == request.Interest
                        && rp.Replay.GameTime >= fromDate
                        && rp.Replay.GameTime < toDate
                      group new { pr.Player, rp } by new { rp.Player.Name, rp.Player.ToonId, rp.Player.RegionId } into g
                      where g.Count() >= 10
                      select new CmdrPlayerInfo()
                      {
                        Name = g.Key.Name,
                        ToonId = g.Key.ToonId,
                        RegionId = g.Key.RegionId,
                        Count = g.Count(),
                        Wins = g.Count(c => c.rp.PlayerResult == PlayerResult.Win),
                        AvgRating = Math.Round(g.Average(s => s.rp.ReplayPlayerRatingInfo.Rating)),
                        AvgGain = Math.Round(g.Average(a => a.rp.ReplayPlayerRatingInfo.RatingChange), 2)
                      };
#pragma warning restore CS8602 // Dereference of a possibly null reference.

        return await group
            .OrderByDescending(o => o.Count)
            .ToListAsync(token);
    }
}