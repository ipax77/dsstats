
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using pax.dsstats.shared;
using pax.dsstats.shared.Interfaces;

namespace pax.dsstats.dbng.Services;

public class WinrateService : IWinrateService
{
    private readonly ReplayContext context;
    private readonly IMemoryCache memoryCache;

    public WinrateService(ReplayContext context, IMemoryCache memoryCache)
    {
        this.context = context;
        this.memoryCache = memoryCache;
    }

    public async Task<WinrateResponse> GetWinrate(WinrateRequest request, CancellationToken token)
    {
        var data = await GetData(request, token);

        if (request.RatingType == RatingType.Std || request.RatingType == RatingType.StdTE)
        {
            data = data.Where(x => (int)x.Commander <= 3).ToList();
        }
        else
        {
            data = data.Where(x => (int)x.Commander > 3).ToList();
        }

        return new()
        {
            Interest = request.Interest,
            WinrateEnts = data,
        };
    }

    private async Task<List<WinrateEnt>> GetData(WinrateRequest request, CancellationToken token)
    {
        (var fromDate, var toDate) = Data.TimeperiodSelected(request.TimePeriod);

        var group = request.Interest == Commander.None ?
                    from r in context.Replays
                    from rp in r.ReplayPlayers
                    where r.GameTime > fromDate
                        && r.GameTime < toDate
                        && toDate < DateTime.Now.AddDays(-2) ? r.GameTime < toDate : true
                        && r.ReplayRatingInfo!.RatingType == request.RatingType
                        && rp.Duration >= 300
                    group rp by rp.Race into g
                    select new WinrateEnt
                    {
                        Commander = g.Key,
                        Count = g.Count(),
                        Wins = g.Count(s => s.PlayerResult == PlayerResult.Win),
                        AvgRating = Math.Round(g.Average(a => a.ReplayPlayerRatingInfo!.Rating), 2),
                        AvgGain = Math.Round(g.Average(a => a.ReplayPlayerRatingInfo!.RatingChange), 2),
                    } :
                    from r in context.Replays
                    from rp in r.ReplayPlayers
                    where r.GameTime > fromDate
                        && r.GameTime < toDate
                        && toDate < DateTime.Now.AddDays(-2) ? r.GameTime < toDate : true
                        && r.ReplayRatingInfo!.RatingType == request.RatingType
                        && rp.Duration >= 300
                        && rp.Race == request.Interest
                    group rp by rp.OppRace into g
                    select new WinrateEnt
                    {
                        Commander = g.Key,
                        Count = g.Count(),
                        Wins = g.Count(s => s.PlayerResult == PlayerResult.Win),
                        AvgRating = Math.Round(g.Average(a => a.ReplayPlayerRatingInfo!.Rating), 2),
                        AvgGain = Math.Round(g.Average(a => a.ReplayPlayerRatingInfo!.RatingChange), 2),
                    };

        return await group.ToListAsync(token);
    }
}

