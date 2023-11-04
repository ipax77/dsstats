using dsstats.db;
using dsstats.shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace dsstats.services;

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

        var replays = context.Replays
            .Where(x => x.GameTime > startDate
                && x.ReplayRating != null
                && x.ReplayRating.LeaverType == LeaverType.None
                && x.ReplayRating.RatingType == request.RatingType);

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

        var group = request.Interest == Commander.None
                    ?
                        //from r in replays
                        //from rp in r.ReplayPlayers
                        from rp in replayPlayers
                        group rp by rp.Race into g
                        select new CmdrStrengthItem()
                        {
                            Commander = g.Key,
                            Matchups = g.Count(),
                            AvgRating = Math.Round(g.Average(a => a.ReplayPlayerRatingInfo!.Rating), 2),
                            AvgRatingGain = Math.Round(g.Average(a => a.ReplayPlayerRatingInfo!.RatingChange), 2),
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
                            AvgRating = Math.Round(g.Average(a => a.ReplayPlayerRatingInfo!.Rating), 2),
                            AvgRatingGain = Math.Round(g.Average(a => a.ReplayPlayerRatingInfo!.RatingChange), 2),
                            Wins = g.Sum(c => c.PlayerResult == PlayerResult.Win ? 1 : 0)
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

    private async Task<CmdrStrengthResult> ProduceComboCmdrStrengthResult(CmdrStrengthRequest request, CancellationToken token)
    {
        (var startDate, var endDate) = Data.TimeperiodSelected(request.TimePeriod);

        var replays = context.Replays
            .Where(x => x.GameTime > startDate
                && x.ComboReplayRating!.LeaverType == LeaverType.None
                && x.ComboReplayRating!.RatingType == request.RatingType);

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

        var group = request.Interest == Commander.None
                    ?
                        from rp in replayPlayers
                        group rp by rp.Race into g
                        select new CmdrStrengthItem()
                        {
                            Commander = g.Key,
                            Matchups = g.Count(),
                            AvgRating = Math.Round(g.Average(a => a.ComboReplayPlayerRating!.Rating), 2),
                            AvgRatingGain = Math.Round(g.Average(a => a.ComboReplayPlayerRating!.Change), 2),
                            Wins = g.Sum(c => c.PlayerResult == PlayerResult.Win ? 1 : 0)
                        }
                    :
                        from rp in replayPlayers
                        where rp.Race == request.Interest
                        group rp by rp.OppRace into g
                        select new CmdrStrengthItem()
                        {
                            Commander = g.Key,
                            Matchups = g.Count(),
                            AvgRating = Math.Round(g.Average(a => a.ComboReplayPlayerRating!.Rating), 2),
                            AvgRatingGain = Math.Round(g.Average(a => a.ComboReplayPlayerRating!.Change), 2),
                            Wins = g.Sum(c => c.PlayerResult == PlayerResult.Win ? 1 : 0)
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
