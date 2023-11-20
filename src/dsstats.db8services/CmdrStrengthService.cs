using dsstats.db8;
using dsstats.shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace dsstats.db8services;

public partial class CmdrStrengthService
{
    private readonly ReplayContext context;
    private readonly IMemoryCache memoryCache;

    public CmdrStrengthService(ReplayContext context, IMemoryCache memoryCache)
    {
        this.context = context;
        this.memoryCache = memoryCache;
    }

    public async Task<CmdrStrengthResult> GetCmdrStrength(CmdrStrengthRequest request, CancellationToken token = default)
    {
        var memKey = request.GenMemKey();
        if (!memoryCache.TryGetValue(memKey, out CmdrStrengthResult? result)
            || result == null)
        {
            result = await ProduceCmdrStrengthResult(request, token);
            memoryCache.Set(memKey, result, TimeSpan.FromHours(24));
        }
        return result;
    }

    private async Task<CmdrStrengthResult> ProduceCmdrStrengthResult(CmdrStrengthRequest request, CancellationToken token)
    {
        (var startDate, var endDate) = Data.TimeperiodSelected(request.TimePeriod);
        var tillDate = DateTime.Today.AddDays(-2);

        var group = request.Interest == Commander.None
                    ?
                        from r in context.Replays
                        from rp in r.ReplayPlayers
                        join rr in context.ReplayRatings on r.ReplayId equals rr.ReplayId
                        join rpr in context.RepPlayerRatings on rp.ReplayPlayerId equals rpr.ReplayPlayerId
                        where r.GameTime > startDate
                         && (endDate > tillDate || r.GameTime <= endDate)
                         && rr.LeaverType == LeaverType.None
                         && rr.RatingType == request.RatingType
                         && (request.Team != TeamRequest.Team1 || rp.GamePos < 4)
                         && (request.Team != TeamRequest.Team2 || rp.GamePos > 3)
                        group new { rp, rpr } by rp.Race into g
                        select new CmdrStrengthItem()
                        {
                            Commander = g.Key,
                            Matchups = g.Count(),
                            AvgRating = Math.Round(g.Average(a => a.rpr.Rating), 2),
                            AvgRatingGain = Math.Round(g.Average(a => a.rpr.RatingChange), 2),
                            Wins = g.Sum(c => c.rp.PlayerResult == PlayerResult.Win ? 1 : 0)
                        }
                    :
                        from r in context.Replays
                        from rp in r.ReplayPlayers
                        join rr in context.ReplayRatings on r.ReplayId equals rr.ReplayId
                        join rpr in context.RepPlayerRatings on rp.ReplayPlayerId equals rpr.ReplayPlayerId
                        where r.GameTime > startDate
                         && (endDate > tillDate || r.GameTime <= endDate)
                         && rr.LeaverType == LeaverType.None
                         && rr.RatingType == request.RatingType
                         && (request.Team != TeamRequest.Team1 || rp.GamePos < 4)
                         && (request.Team != TeamRequest.Team2 || rp.GamePos > 3)
                         && rp.Race == request.Interest
                        group new { rp, rpr } by rp.OppRace into g
                        select new CmdrStrengthItem()
                        {
                            Commander = g.Key,
                            Matchups = g.Count(),
                            AvgRating = Math.Round(g.Average(a => a.rpr.Rating), 2),
                            AvgRatingGain = Math.Round(g.Average(a => a.rpr.RatingChange), 2),
                            Wins = g.Sum(c => c.rp.PlayerResult == PlayerResult.Win ? 1 : 0)
                        }
           ;

        var items = await group.ToListAsync(token);

        if (request.RatingType == RatingType.Cmdr || request.RatingType == RatingType.CmdrTE)
        {
            items = items.Where(x => (int)x.Commander > 3).ToList();
        }
        else if (request.RatingType == RatingType.Std || request.RatingType == RatingType.StdTE)
        {
            items = items.Where(x => (int)x.Commander <= 3).ToList();
        }

        return new()
        {
            Items = items
        };
    }


}
