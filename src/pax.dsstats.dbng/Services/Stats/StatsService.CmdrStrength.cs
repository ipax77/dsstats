using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using pax.dsstats.dbng.Extensions;
using pax.dsstats.shared;

namespace pax.dsstats.dbng.Services;

public partial class StatsService
{
    public async Task<CmdrStrengthResult> GetCmdrStrengthResults(CmdrStrengthRequest request, CancellationToken token)
    {
        var memKey = request.GenMemKey();
        if (!memoryCache.TryGetValue(memKey, out CmdrStrengthResult result))
        {
            result = await ProduceCmdrStrengthResult(request, token);
            memoryCache.Set(memKey, result, TimeSpan.FromHours(24));
        }
        return result;
    }

    private async Task<CmdrStrengthResult> ProduceCmdrStrengthResult(CmdrStrengthRequest request, CancellationToken token)
    {
        (var startDate, var endDate) = Data.TimeperiodSelected(request.TimePeriod);

        var replays = context.Replays
            .Where(x => x.GameTime > startDate
                && x.ReplayRatingInfo != null
                && x.ReplayRatingInfo.LeaverType == LeaverType.None
                && x.ReplayRatingInfo.RatingType == request.RatingType);

        if (endDate != DateTime.MinValue && (DateTime.Today - endDate).TotalDays > 2)
        {
            replays = replays.Where(x => x.GameTime < endDate);
        }

        var replayPlayers = replays.SelectMany(s => s.ReplayPlayers);

        if (request.Team == TeamRequest.Team1)
        {
            replayPlayers = replayPlayers.Where(x => x.GamePos < 4);
        }
        else if (request.Team == TeamRequest.Team2)
        {
            replayPlayers = replayPlayers.Where(x => x.GamePos > 3);
        }

#pragma warning disable CS8602 // Dereference of a possibly null reference.
        var group = request.Interest == Commander.None
                    ?
                        //from r in replays
                        //from rp in r.ReplayPlayers
                        from rp in replayPlayers
                        group rp by  rp.Race into g
                        select new CmdrStrengthItem()
                        {
                            Commander = g.Key,
                            Matchups = g.Count(),
                            AvgRating = Math.Round(g.Average(a => a.ReplayPlayerRatingInfo.Rating), 2),
                            AvgRatingGain = Math.Round(g.Average(a => a.ReplayPlayerRatingInfo.RatingChange), 2),
                            Wins = g.Sum(c => c.PlayerResult == PlayerResult.Win ? 1 : 0)
                        }
                    :
                        //from r in replays
                        //from rp in r.ReplayPlayers
                        from rp in replayPlayers
                        where rp.Race == request.Interest
                        group rp by rp.OppRace into g
                        select new CmdrStrengthItem()
                        {
                            Commander = g.Key,
                            Matchups = g.Count(),
                            AvgRating = Math.Round(g.Average(a => a.ReplayPlayerRatingInfo.Rating), 2),
                            AvgRatingGain = Math.Round(g.Average(a => a.ReplayPlayerRatingInfo.RatingChange), 2),
                            Wins = g.Sum(c => c.PlayerResult == PlayerResult.Win ? 1 : 0)
                        }
           ;

#pragma warning restore CS8602 // Dereference of a possibly null reference.

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
