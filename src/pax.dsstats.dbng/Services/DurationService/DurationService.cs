
using Microsoft.EntityFrameworkCore;
using pax.dsstats.shared;
using MathNet.Numerics.Interpolation;
using pax.dsstats.shared.Interfaces;
using MathNet.Numerics;

namespace pax.dsstats.dbng.Services;

public class DurationService : IDurationService
{
    private readonly ReplayContext context;

    public DurationService(ReplayContext context)
    {
        this.context = context;
    }

    public async Task<DurationResponse> GetDuration(DurationRequest request, CancellationToken token = default)
    {
        if (request.Commander == Commander.None)
        {
            request.Commander = Commander.Abathur;
        }

        var results = await GetDurationRangeData(request, token);
        // var results = await GetEfDurationRangeData(request, token);

        var chartData = GetChartData(results);

        return new DurationResponse()
        {
            Commander = request.Commander,
            ChartData = chartData,
            Results = results
        };
    }

    private async Task<List<DRangeResult>> GetDurationRangeData(DurationRequest request, CancellationToken token)
    {
        (var from, var to) = Data.TimeperiodSelected(request.TimePeriod);

        var sql =
            $@"SELECT CASE
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
                        END as drange, count(*) as count, count(CASE WHEN rp.PlayerResult = 1 THEN 1 END) as wins
                FROM Replays as r
                INNER JOIN ReplayPlayers AS rp on rp.ReplayId = r.ReplayId
                WHERE r.GameTime > '{from:yyyy-MM-dd}' AND r.DefaultFilter = 1 AND rp.Race = {(int)request.Commander}
                    {(to < DateTime.Today.AddDays(-2) ? $"AND r.GameTime < '{to:yyyy-MM-dd}'" : "")}
                GROUP BY drange;            
            ";

        var result = await context.DRangeResults
            .FromSqlRaw(sql)
            .ToListAsync(token);

        return result;
    }

    private async Task<List<DRangeResult>> GetEfDurationRangeData(DurationRequest request, CancellationToken token)
    {
        (DateTime fromDate, DateTime toDate) = Data.TimeperiodSelected(request.TimePeriod);

        var query = from r in context.Replays
                    join rp in context.ReplayPlayers on r.ReplayId equals rp.ReplayId
                    where r.GameTime > fromDate
                        && r.DefaultFilter
                        && rp.Race == request.Commander
                        && (toDate < DateTime.Today.AddDays(-2) ? r.GameTime < toDate.Date : true)
                    group rp by new
                    {
                        DRange = r.Duration < 480 ? 1 :
                                r.Duration < 660 ? 2 :
                                r.Duration < 840 ? 3 :
                                r.Duration < 1020 ? 4 :
                                r.Duration < 1200 ? 5 :
                                r.Duration < 1380 ? 6 :
                                r.Duration < 1560 ? 7 :
                                r.Duration < 1740 ? 8 :
                                r.Duration < 1920 ? 9 :
                                10
                    } into g
                    select new DRangeResult
                    {
                        DRange = g.Key.DRange,
                        Count = g.Count(),
                        Wins = g.Count(rp => rp.PlayerResult == PlayerResult.Win)
                    };

        return await query.ToListAsync(token);
    }

    private static ChartData GetChartData(List<DRangeResult> results)
    {
        if (!results.Any())
        {
            return new();
        }

        List<double> xValues = new();
        List<double> yValues = new();

        for (int i = 0; i < results.Count; i++)
        {
            var result = results[i];
            xValues.Add(i);
            yValues.Add(result.Wins * 100.0 / result.Count);
        }

        int order = Math.Min(4, yValues.Count - 1);

        var poly = Fit.PolynomialFunc(xValues.ToArray(), yValues.ToArray(), 4);

        return new ChartData()
        {
            Labels = results.Select(s => GetDRangeLabel(s.DRange)).ToList(),
            Data = xValues.Select(s => Math.Round(poly(s), 2)).ToList()
        };
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
