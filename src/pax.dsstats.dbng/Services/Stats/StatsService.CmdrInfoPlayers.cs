
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

        int limit = 10 * 2;
        if (request.RatingType == RatingType.CmdrTE)
        {
            limit = 2 * 2;
        }

#pragma warning disable CS8602 // Dereference of a possibly null reference.        
        var group = request.Uploaders ? from pr in context.PlayerRatings
                      from rp in pr.Player.ReplayPlayers
                      join tm in context.ReplayPlayers on new { rp.ReplayId, rp.Team } equals new { tm.ReplayId, tm.Team }
                      join rpr in context.RepPlayerRatings on tm.ReplayPlayerId equals rpr.ReplayPlayerId
                      where pr.RatingType == request.RatingType
                        && rp.Replay.ReplayRatingInfo.RatingType == request.RatingType
                        && rp.Race == request.Interest
                        && rp.Replay.GameTime >= fromDate
                        && rp.Replay.GameTime < toDate
                        && rp.IsUploader
                        && tm.ReplayPlayerId != rp.ReplayPlayerId
                      group new { pr.Player, rp, rpr } by new { rp.Player.Name, rp.Player.ToonId, rp.Player.RegionId, rp.Player.RealmId } into g
                      where g.Count() >= limit
                      select new CmdrPlayerInfo()
                      {
                        Name = g.Key.Name,
                        ToonId = g.Key.ToonId,
                        RegionId = g.Key.RegionId,
                        RealmId = g.Key.RealmId,
                        Count = g.Count() / 2,
                        Wins = g.Count(c => c.rp.PlayerResult == PlayerResult.Win) / 2,
                        AvgRating = Math.Round(g.Average(s => s.rp.ReplayPlayerRatingInfo.Rating)),
                        AvgGain = Math.Round(g.Average(a => a.rp.ReplayPlayerRatingInfo.RatingChange), 2),
                        TeamRating = Math.Round(g.Average(a => a.rpr.Rating))
                      }
                    : from pr in context.PlayerRatings
                    from rp in pr.Player.ReplayPlayers
                    join tm in context.ReplayPlayers on new { rp.ReplayId, rp.Team } equals new { tm.ReplayId, tm.Team }
                    join rpr in context.RepPlayerRatings on tm.ReplayPlayerId equals rpr.ReplayPlayerId
                    where pr.RatingType == request.RatingType
                      && rp.Replay.ReplayRatingInfo.RatingType == request.RatingType
                      && rp.Race == request.Interest
                      && rp.Replay.GameTime >= fromDate
                      && rp.Replay.GameTime < toDate
                      && tm.ReplayPlayerId != rp.ReplayPlayerId
                    group new { pr.Player, rp, rpr } by new { rp.Player.Name, rp.Player.ToonId, rp.Player.RegionId, rp.Player.RealmId } into g
                    where g.Count() >= limit
                    select new CmdrPlayerInfo()
                    {
                        Name = g.Key.Name,
                        ToonId = g.Key.ToonId,
                        RegionId = g.Key.RegionId,
                        RealmId = g.Key.RealmId,
                        Count = g.Count() / 2,
                        Wins = g.Count(c => c.rp.PlayerResult == PlayerResult.Win) / 2,
                        AvgRating = Math.Round(g.Average(s => s.rp.ReplayPlayerRatingInfo.Rating)),
                        AvgGain = Math.Round(g.Average(a => a.rp.ReplayPlayerRatingInfo.RatingChange), 2),
                        TeamRating = Math.Round(g.Average(a => a.rpr.Rating))
                    };
                    #pragma warning restore CS8602 // Dereference of a possibly null reference.

        var data = await group
            .OrderByDescending(o => o.Count)
            .ToListAsync(token);
        return data;
    }
}