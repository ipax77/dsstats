
using Microsoft.EntityFrameworkCore;
using pax.dsstats.shared;
using pax.dsstats.shared.Interfaces;
using MathNet.Numerics;
using Microsoft.Extensions.Caching.Memory;
using pax.dsstats.dbng.Extensions;
using Microsoft.Extensions.Logging;

namespace pax.dsstats.dbng.Services;

public class DurationService : IDurationService
{
    private readonly ReplayContext context;
    private readonly IMemoryCache memoryCache;
    private readonly ILogger<DurationService> logger;

    public DurationService(ReplayContext context, IMemoryCache memoryCache, ILogger<DurationService> logger)
    {
        this.context = context;
        this.memoryCache = memoryCache;
        this.logger = logger;
    }

    public async Task<DurationResponse> GetDuration(DurationRequest request, CancellationToken token = default)
    {
        var mamKey = request.GenMemKey();
        if (!memoryCache.TryGetValue(mamKey, out DurationResponse response))
        {
            response = await ProduceDuration(request, token);
            memoryCache.Set(mamKey, response, TimeSpan.FromHours(24));
        }
        return response;
    }

    private async Task<DurationResponse> ProduceDuration(DurationRequest request, CancellationToken token = default)
    {
        var results = request.WithRating ?
            await GetDurationRangeDataWithRating(request, token)
            : await GetDurationRangeData(request, token);

        try
        {
            var chartDatas = GetChartData(results, request.WithRating);
            return new DurationResponse()
            {
                ChartDatas = chartDatas
            };
        }
        catch (Exception ex)
        {
            logger.LogError($"failed producing duration: {ex.Message}");
        }
        return new();
    }

    private async Task<List<DRangeResult>> GetDurationRangeData(DurationRequest request, CancellationToken token)
    {
        (var from, var to) = Data.TimeperiodSelected(request.TimePeriod);

        var sql =
            $@"SELECT rp.Race,
                     CASE
                        WHEN r.Duration < 480 THEN 1
                        WHEN r.Duration < 660 THEN 2
                        WHEN r.Duration < 840 THEN 3
                        WHEN r.Duration < 1020 THEN 4
                        WHEN r.Duration < 1200 THEN 5
                        WHEN r.Duration < 1380 THEN 6
                        WHEN r.Duration < 1560 THEN 7
                        WHEN r.Duration < 1740 THEN 8
                        WHEN r.Duration < 1920 THEN 9        
                        ELSE 10
                        END as drange, count(*) as count, CAST(count(CASE WHEN rp.PlayerResult = 1 THEN 1 END) AS DECIMAL) as winsOrRating
                FROM Replays as r
                INNER JOIN ReplayPlayers AS rp on rp.ReplayId = r.ReplayId
                WHERE r.GameTime > '{from:yyyy-MM-dd}' AND r.DefaultFilter = 1
                    {(to < DateTime.Today.AddDays(-2) ? $"AND r.GameTime < '{to:yyyy-MM-dd}'" : "")}
                    {(request.WithBrawl ? "" : "AND r.GameMode IN (3,4,7)")}
                GROUP BY drange, rp.Race;            
            ";

        var result = await context.DRangeResults
            .FromSqlRaw(sql)
            .ToListAsync(token);

        return result;
    }

    private async Task<List<DRangeResult>> GetDurationRangeDataWithRating(DurationRequest request, CancellationToken token)
    {
        (var from, var to) = Data.TimeperiodSelected(request.TimePeriod);

        var sql =
            $@"SELECT rp.Race,
                     CASE
                        WHEN r.Duration < 480 THEN 1
                        WHEN r.Duration < 660 THEN 2
                        WHEN r.Duration < 840 THEN 3
                        WHEN r.Duration < 1020 THEN 4
                        WHEN r.Duration < 1200 THEN 5
                        WHEN r.Duration < 1380 THEN 6
                        WHEN r.Duration < 1560 THEN 7
                        WHEN r.Duration < 1740 THEN 8
                        WHEN r.Duration < 1920 THEN 9        
                        ELSE 10
                        END as drange, count(*) as count, ROUND(AVG(rpr.RatingChange), 2) as winsOrRating
                FROM Replays as r
                INNER JOIN ReplayPlayers AS rp on rp.ReplayId = r.ReplayId
                INNER JOIN RepPlayerRatings AS rpr on rpr.ReplayPlayerId = rp.ReplayPlayerId
                WHERE r.GameTime > '{from:yyyy-MM-dd}' AND r.DefaultFilter = 1
                    {(to < DateTime.Today.AddDays(-2) ? $"AND r.GameTime < '{to:yyyy-MM-dd}'" : "")}
                    {(request.RatingType == RatingType.Std ? "AND r.GameMode = 7" : "AND r.GameMode IN (3,4)")}
                GROUP BY drange, rp.Race;            
            ";

        var result = await context.DRangeResults
            .FromSqlRaw(sql)
            .ToListAsync(token);

        return result;
    }

    private static List<ChartData> GetChartData(List<DRangeResult> results, bool withRating)
    {
        if (!results.Any())
        {
            return new();
        }

        List<ChartData> datas = new();
        double[] xValues = new double[10] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };


        foreach (var commander in Data.GetCommanders(Data.CmdrGet.NoNone))
        {
            List<double> yValues = new();
            List<int> counts = new();

            foreach (var result in results.Where(x => x.Race == (int)commander))
            {
                yValues.Add(withRating ? result.WinsOrRating :
                        result.Count == 0 ? 0 : result.WinsOrRating * 100.0 / result.Count);
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

    private static string GetDRangeLabel(int drange)
    {
        return drange switch
        {
            1 => "5 - 8",
            2 => "8 - 11",
            3 => "11 - 14",
            4 => "14 - 17",
            5 => "17 - 20",
            6 => "20 - 23",
            7 => "23 - 26",
            8 => "26 - 29",
            9 => "29 - 32",
            _ => "32+"
        };
    }

}
