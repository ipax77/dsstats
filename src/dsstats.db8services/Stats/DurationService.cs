using dsstats.db8;
using dsstats.shared;
using dsstats.shared.Interfaces;
using MathNet.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace dsstats.db8services;

public class DurationService : IDurationService
{
    private readonly ReplayContext context;
    private readonly IMemoryCache memoryCache;

    public DurationService(ReplayContext context, IMemoryCache memoryCache)
    {
        this.context = context;
        this.memoryCache = memoryCache;
    }

    public async Task<DurationResponse> GetDuration(StatsRequest request, CancellationToken token = default)
    {
        var mamKey = request.GenMemKey("Duration");
        if (!memoryCache.TryGetValue(mamKey, out DurationResponse? response)
            || response is null)
        {
            response = await ProduceDuration(request, token);
            memoryCache.Set(mamKey, response, TimeSpan.FromHours(24));
        }
        return response;
    }

    private async Task<DurationResponse> ProduceDuration(StatsRequest request, CancellationToken token = default)
    {
        var data = request.ComboRating ?
            await GetComboDurationRangeData(request, token)
            : await GetDurationRangeData(request, token);

        var chartdata = GetChartData(data);

        return new()
        {
            ChartDatas = chartdata,
        };
    }

    private static List<ChartData> GetChartData(List<DRangeResult> results)
    {
        if (results.Count == 0)
        {
            return new();
        }

        List<ChartData> datas = new();
        double[] xValues = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9];


        foreach (var commander in Data.GetCommanders(Data.CmdrGet.NoNone))
        {
            List<double> yValues = new();
            List<int> counts = new();

            foreach (var result in results.Where(x => x.Race == commander))
            {
                yValues.Add(result.AvgGain);
                counts.Add(result.Count);
            }

            if (yValues.Count != 10)
            {
                continue;
            }

            int order = Math.Min(4, yValues.Count - 1);

            var poly = Fit.PolynomialFunc(xValues, yValues.ToArray(), order);

            datas.Add(new ChartData()
            {
                Commander = commander,
                Data = yValues.ToList(),
                NiceData = xValues.Select(s => Math.Round(poly(s), 2)).ToList(),
                Counts = counts
            });
        }
        return datas;
    }

    private async Task<List<DRangeResult>> GetDurationRangeData(StatsRequest request, CancellationToken token)
    {
        (var fromDate, var toDate) = request.GetTimeLimits();
        var tillDate = DateTime.Today.AddDays(-2);

        var limits = request.GetFilterLimits();
        List<GameMode> gameModes = request.RatingType == RatingType.Std ?
            new List<GameMode>() { GameMode.Standard }
            : new List<GameMode>() { GameMode.Commanders, GameMode.CommandersHeroic };

        var query = from r in context.Replays
                    from rp in r.ReplayPlayers
                    join rpr in context.RepPlayerRatings on rp.ReplayPlayerId equals rpr.ReplayPlayerId
                    join rr in context.ReplayRatings on r.ReplayId equals rr.ReplayId
                    where r.GameTime >= fromDate
                     && (toDate > tillDate || r.GameTime <= toDate)
                     && gameModes.Contains(r.GameMode)
                     && r.Duration > 300
                     && (limits.FromRating <= 0 || rpr.Rating >= limits.FromRating)
                     && (limits.ToRating <= 0 || rpr.Rating <= limits.ToRating)
                     && (limits.FromExp2Win <= 0 || rr.ExpectationToWin >= limits.FromExp2Win)
                     && (limits.ToExp2Win <= 0 || rr.ExpectationToWin <= limits.ToExp2Win)
                     && (!request.WithoutLeavers || rr.LeaverType == LeaverType.None)
                    group new { r, rpr } by new 
                    { 
                        rp.Race,
                        Drange = (r.Duration < 480 ? 1 :
                            r.Duration < 660 ? 2 :
                            r.Duration < 840 ? 3 :
                            r.Duration < 1020 ? 4 :
                            r.Duration < 1200 ? 5 :
                            r.Duration < 1380 ? 6 :
                            r.Duration < 1560 ? 7 :
                            r.Duration < 1740 ? 8 :
                            r.Duration < 1920 ? 9 : 10)
                    } into g
                    select new DRangeResult()
                    {
                        Race = g.Key.Race,
                        DRange = g.Key.Drange,
                        Count = g.Count(),
                        AvgGain = Math.Round(g.Average(a => a.rpr.RatingChange), 2)
                    };
        return await query.ToListAsync(token);
    }

    private async Task<List<DRangeResult>> GetComboDurationRangeData(StatsRequest request, CancellationToken token)
    {
        (var fromDate, var toDate) = request.GetTimeLimits();
        var tillDate = DateTime.Today.AddDays(-2);

        var limits = request.GetFilterLimits();
        List<GameMode> gameModes = request.RatingType == RatingType.Std ?
            new List<GameMode>() { GameMode.Standard }
            : new List<GameMode>() { GameMode.Commanders, GameMode.CommandersHeroic };

        var query = from r in context.Replays
                    from rp in r.ReplayPlayers
                    join rpr in context.ComboReplayPlayerRatings on rp.ReplayPlayerId equals rpr.ReplayPlayerId
                    join rr in context.ComboReplayRatings on r.ReplayId equals rr.ReplayId
                    where r.GameTime >= fromDate
                     && (toDate > tillDate || r.GameTime <= toDate)
                     && gameModes.Contains(r.GameMode)
                     && r.Duration > 300
                     && (limits.FromRating <= 0 || rpr.Rating >= limits.FromRating)
                     && (limits.ToRating <= 0 || rpr.Rating <= limits.ToRating)
                     && (limits.FromExp2Win <= 0 || rr.ExpectationToWin >= limits.FromExp2Win)
                     && (limits.ToExp2Win <= 0 || rr.ExpectationToWin <= limits.ToExp2Win)
                     && (!request.WithoutLeavers || rr.LeaverType == LeaverType.None)
                    group new { r, rpr } by new
                    {
                        rp.Race,
                        Drange = (r.Duration < 480 ? 1 :
                            r.Duration < 660 ? 2 :
                            r.Duration < 840 ? 3 :
                            r.Duration < 1020 ? 4 :
                            r.Duration < 1200 ? 5 :
                            r.Duration < 1380 ? 6 :
                            r.Duration < 1560 ? 7 :
                            r.Duration < 1740 ? 8 :
                            r.Duration < 1920 ? 9 : 10)
                    } into g
                    select new DRangeResult()
                    {
                        Race = g.Key.Race,
                        DRange = g.Key.Drange,
                        Count = g.Count(),
                        AvgGain = Math.Round(g.Average(a => a.rpr.Change), 2)
                    };
        return await query.ToListAsync(token);
    }
}

public record DRangeResult
{
    public Commander Race { get; set; }
    public int DRange { get; set; }
    public int Count { get; set; }
    public double AvgGain { get; set; }
}