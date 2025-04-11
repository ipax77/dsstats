using dsstats.shared;
using dsstats.shared8;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace dsstats.db.Services.Stats;

public class WinrateService(DsstatsContext context, ILogger<WinrateService> logger)
{
    public async Task<List<WinrateEnt>?> GetData(StatsRequest request, CancellationToken token)
    {
        (var fromDate, var toDate) = request.GetTimeLimits();
        var tillDate = toDate.AddDays(-2);

        var limits = request.GetFilterLimits();
        var ratingType = RatingNgType.Global;

        var group = request.Interest == Commander.None ?
                    from r in context.Replays
                    from rp in r.ReplayPlayers
                    join rr in context.ReplayRatings on r.ReplayId equals rr.ReplayId
                    join rpr in context.ReplayPlayerRatings on rp.ReplayPlayerId equals rpr.ReplayPlayerId
                    where r.GameTime > fromDate
                     && (toDate > tillDate || r.GameTime <= toDate)
                     && r.Duration > 300
                     && rr.RatingType == ratingType
                     && rpr.RatingType == ratingType
                     && (limits.FromRating <= 0 || rpr.Rating >= limits.FromRating)
                     && (limits.ToRating <= 0 || rpr.Rating <= limits.ToRating)
                     && (limits.FromExp2Win <= 0 || rr.ExpectationToWin >= (decimal)limits.FromExp2Win)
                     && (limits.ToExp2Win <= 0 || rr.ExpectationToWin <= (decimal)limits.ToExp2Win)
                     && (!request.WithoutLeavers || rr.LeaverType == LeaverType.None)
                    group new { rp, rr, rpr, r } by rp.Race into g
                    select new WinrateEnt()
                    {
                        Commander = g.Key,
                        Count = g.Count(),
                        AvgRating = Math.Round(g.Average(a => a.rpr.Rating), 2),
                        AvgGain = (double)Math.Round(g.Average(a => a.rpr.Change), 2),
                        Wins = g.Sum(s => s.rp.PlayerResult == PlayerResult.Win ? 1 : 0),
                        Replays = g.Select(s => s.r.ReplayId).Distinct().Count()
                    }
                    :
                    from r in context.Replays
                    from rp in r.ReplayPlayers
                    join rr in context.ReplayRatings on r.ReplayId equals rr.ReplayId
                    join rpr in context.ReplayPlayerRatings on rp.ReplayPlayerId equals rpr.ReplayPlayerId
                    where r.GameTime > fromDate
                     && (toDate > tillDate || r.GameTime <= toDate)
                     && r.Duration > 300
                     && rr.RatingType == ratingType
                     && rpr.RatingType == ratingType
                     && (limits.FromRating <= 0 || rpr.Rating >= limits.FromRating)
                     && (limits.ToRating <= 0 || rpr.Rating <= limits.ToRating)
                     && (limits.FromExp2Win <= 0 || rr.ExpectationToWin >= (decimal)limits.FromExp2Win)
                     && (limits.ToExp2Win <= 0 || rr.ExpectationToWin <= (decimal)limits.ToExp2Win)
                     && rp.Race == request.Interest
                     && (!request.WithoutLeavers || rr.LeaverType == LeaverType.None)
                    group new { rp, rr, rpr, r } by rp.Opponent!.Race into g
                    select new WinrateEnt()
                    {
                        Commander = g.Key,
                        Count = g.Count(),
                        AvgRating = Math.Round(g.Average(a => a.rpr.Rating), 2),
                        AvgGain = (double)Math.Round(g.Average(a => a.rpr.Change), 2),
                        Wins = g.Sum(s => s.rp.PlayerResult == PlayerResult.Win ? 1 : 0),
                        Replays = g.Select(s => s.r.ReplayId).Distinct().Count()
                    };

        var list = await group.ToListAsync();
        return list;
    }
}