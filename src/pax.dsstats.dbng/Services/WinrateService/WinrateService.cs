
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using pax.dsstats.dbng.Extensions;
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
        var memKey = request.GenMemKey();

        if (!memoryCache.TryGetValue(memKey, out WinrateResponse response)) 
        { 
            response = await ProduceWinrate(request, token);
            memoryCache.Set(memKey, response, TimeSpan.FromHours(24));
        }

        return response;
    }

    private async Task<WinrateResponse> ProduceWinrate(WinrateRequest request, CancellationToken token)
    {
        var data = await GetDataFromRaw(request, token);

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

    private async Task<List<WinrateEnt>> GetDataFromRaw(WinrateRequest request, CancellationToken token)
    {
        (var fromDate, var toDate) = Data.TimeperiodSelected(request.TimePeriod);

        var sql = request.Interest == Commander.None ?
            $@"
                SELECT 
                    rp.Race as commander,
	                count(*) as count,
                    round(avg(rpr.Rating), 2) as avgrating,
                    round(avg(rpr.RatingChange), 2) as avggain,
                    sum(CASE WHEN rp.PlayerResult = 1 THEN 1 ELSE 0 END) as wins
                FROM Replays as r
                INNER JOIN ReplayRatings as rr on rr.ReplayId = r.ReplayId
                INNER JOIN ReplayPlayers AS rp on rp.ReplayId = r.ReplayId
                INNER JOIN RepPlayerRatings AS rpr on rpr.ReplayPlayerId = rp.ReplayPlayerId
                WHERE rr.RatingType = {(int)request.RatingType}
                 AND r.GameTime > '{fromDate.ToString("yyyy-MM-dd")}'
                 {(toDate < DateTime.Today.AddDays(-2) ? $"AND r.GameTime < '{toDate.ToString("yyyy-MM-dd")}'" : "")}
                 AND rp.Duration > 300
                 {(request.FromRating > Data.MinBuildRating ? $"AND rpr.Rating >= {request.FromRating}" : "")}
                 {(request.ToRating != 0 && request.ToRating < Data.MaxBuildRating ? $"AND rpr.Rating <= {request.ToRating}" : "")}
                GROUP BY rp.Race;
            "
            : 
            $@"
                SELECT 
                    rp.OppRace as commander,
	                count(*) as count,
                    round(avg(rpr.Rating), 2) as avgrating,
                    round(avg(rpr.RatingChange), 2) as avggain,
                    sum(CASE WHEN rp.PlayerResult = 1 THEN 1 ELSE 0 END) as wins
                FROM Replays as r
                INNER JOIN ReplayRatings as rr on rr.ReplayId = r.ReplayId
                INNER JOIN ReplayPlayers AS rp on rp.ReplayId = r.ReplayId
                INNER JOIN RepPlayerRatings AS rpr on rpr.ReplayPlayerId = rp.ReplayPlayerId
                WHERE rr.RatingType = {(int)request.RatingType}
                 AND r.GameTime > '{fromDate.ToString("yyyy-MM-dd")}'
                 {(toDate < DateTime.Today.AddDays(-2) ? $"AND r.GameTime < '{toDate.ToString("yyyy-MM-dd")}'" : "")}
                 AND rp.Duration > 300
                 AND rp.Race = {(int)request.Interest}
                 {(request.FromRating > Data.MinBuildRating ? $"AND rpr.Rating >= {request.FromRating}" : "")}
                 {(request.ToRating != 0 && request.ToRating < Data.MaxBuildRating ? $"AND rpr.Rating <= {request.ToRating}" : "")}
                GROUP BY rp.OppRace;
            ";

        var result = await context.WinrateEnts
            .FromSqlRaw(sql)
            .ToListAsync(token);

        return result;
    }
}

