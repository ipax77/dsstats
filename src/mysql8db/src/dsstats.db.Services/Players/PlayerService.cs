using dsstats.shared;
using dsstats.shared8;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace dsstats.db.Services.Players;

public class PlayerService(DsstatsContext context, ILogger<PlayerService> logger)
{
    public async Task GetPlayerStats(PlayerId playerId, RatingNgType ratingNgType, CancellationToken token)
    {
        var ratings = await context.PlayerRatings
            .Where(x => x.Player!.ToonId == playerId.ToonId
                && x.Player.RealmId == playerId.RealmId
                && x.Player.RegionId == playerId.RegionId)
            .ToListAsync(token);
        

    }

    public async Task<List<PlayerCmdrAvgGain>> GetPlayerIdPlayerCmdrAvgGain(PlayerId playerId,
                                                                            RatingNgType ratingType,
                                                                            TimePeriod timePeriod,
                                                                            CancellationToken token)
    {
        (var startTime, var endTime) = Data.TimeperiodSelected(timePeriod);
        bool noEnd = endTime < DateTime.Today.AddDays(-2);

        var group = from p in context.Players
                    from rp in p.ReplayPlayers
                    join r in context.Replays on rp.ReplayId equals r.ReplayId
                    join rr in context.ReplayRatings on r.ReplayId equals rr.ReplayId
                    join rpr in context.ReplayPlayerRatings on rp.ReplayPlayerId equals rpr.ReplayPlayerId
                    where p.ToonId == playerId.ToonId
                        && p.RegionId == playerId.RegionId
                        && p.RealmId == playerId.RealmId
                        && r.GameTime > startTime
                        && (noEnd || rp.Replay!.GameTime < endTime)
                        && rr.RatingType == ratingType
                        && rpr.RatingType == ratingType
                    group new { rp, rpr } by rp.Race into g
                    orderby g.Count() descending
                    select new PlayerCmdrAvgGain
                    {
                        Commander = g.Key,
                        AvgGain = (double)Math.Round(g.Average(a => a.rpr.Change), 2),
                        Count = g.Count(),
                        Wins = g.Count(c => c.rp.PlayerResult == PlayerResult.Win)
                    };

        var items = await group.ToListAsync(token);
        return items;
    }
}
