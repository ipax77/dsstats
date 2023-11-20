
using dsstats.db8;
using dsstats.shared;
using dsstats.shared.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace dsstats.db8services;

public class TimelineService : ITimelineService
{
    private readonly ReplayContext context;
    private readonly IMemoryCache memoryCache;

    public TimelineService(ReplayContext context, IMemoryCache memoryCache)
    {
        this.context = context;
        this.memoryCache = memoryCache;
    }

    public async Task<List<DsUpdateInfo>> GetDsUpdates(TimePeriod timePeriod, CancellationToken token = default)
    {
        (var fromDate, var toDate) = Data.TimeperiodSelected(timePeriod);

        var dsUpdates = await context.DsUpdates
            .Where(x => x.Time >= fromDate)
            .Select(s => new
            {
                s.Commander,
                s.Time,
                s.Change
            })
            .ToListAsync(token);

        List<DsUpdateInfo> infos = new();

        foreach (var update in dsUpdates)
        {
            var info = infos.FirstOrDefault(f => f.Commander == update.Commander && f.Time == update.Time);

            if (info == null)
            {
                info = new() { Commander = update.Commander, Time = update.Time };
                infos.Add(info);
            }
            info.Changes.Add(update.Change);
        }

        return infos;
    }

    public async Task<TimelineResponse> GetTimeline(StatsRequest request, CancellationToken token = default)
    {
        var memKey = request.GenMemKey("Timeline");
        if (!memoryCache.TryGetValue(memKey, out TimelineResponse? response)
            || response is null)
        {
            response = await ProduceTimeline(request, token);
            memoryCache.Set(memKey, response, TimeSpan.FromHours(24));
        }
        return response;
    }

    private async Task<TimelineResponse> ProduceTimeline(StatsRequest request, CancellationToken token = default)
    {
        var data = request.ComboRating ?
            await GetComboData(request, token)
            : await GetData(request, token);

        return new()
        {
            TimeLineEnts = data
        };
    }

    private async Task<List<TimelineEnt>> GetData(StatsRequest request, CancellationToken token)
    {
        (var fromDate, var toDate) = request.GetTimeLimits();
        toDate = new DateTime(toDate.Year, toDate.Month, 1);

        var limits = request.GetFilterLimits();

        var group = from r in context.Replays
                    from rp in r.ReplayPlayers
                    join rpr in context.RepPlayerRatings on rp.ReplayPlayerId equals rpr.ReplayPlayerId
                    join rr in context.ReplayRatings on r.ReplayId equals rr.ReplayId
                    where r.GameTime > fromDate
                        && r.GameTime < toDate
                        && rr.RatingType == request.RatingType
                        && (limits.FromRating <= 0 || rpr.Rating >= limits.FromRating)
                        && (limits.ToRating <= 0 || rpr.Rating <= limits.ToRating)
                        && (limits.FromExp2Win <= 0 || rr.ExpectationToWin >= limits.FromExp2Win)
                        && (limits.ToExp2Win <= 0 || rr.ExpectationToWin <= limits.ToExp2Win)
                        && (!request.WithoutLeavers || rr.LeaverType == LeaverType.None)
                    group new { r, rp, rpr } by new { rp.Race, r.GameTime.Year, r.GameTime.Month } into g
                    select new TimelineEnt
                    {
                        Commander = g.Key.Race,
                        Time = new DateTime(g.Key.Year, g.Key.Month, 1),
                        Count = g.Count(),
                        AvgRating = Math.Round(g.Average(a => a.rpr.Rating), 2),
                        AvgGain = Math.Round(g.Average(a => a.rpr.RatingChange), 2),
                        Wins = g.Count(s => s.rp.PlayerResult == PlayerResult.Win)
                    };

        var list = await group.ToListAsync(token);

        if (request.RatingType == RatingType.Cmdr || request.RatingType == RatingType.CmdrTE)
        {
            return list.Where(x => (int)x.Commander > 3).ToList();
        }
        else
        {
            return list.Where(x => x.Commander != Commander.None && (int)x.Commander <= 3).ToList();
        }
    }

    private async Task<List<TimelineEnt>> GetComboData(StatsRequest request, CancellationToken token)
    {
        (var fromDate, var toDate) = request.GetTimeLimits();

        toDate = new DateTime(toDate.Year, toDate.Month, 1);

        var limits = request.GetFilterLimits();

        var group = from r in context.Replays
                    from rp in r.ReplayPlayers
                    join rpr in context.ComboReplayPlayerRatings on rp.ReplayPlayerId equals rpr.ReplayPlayerId
                    join rr in context.ComboReplayRatings on r.ReplayId equals rr.ReplayId
                    where r.GameTime > fromDate
                        && r.GameTime < toDate
                        && rr.RatingType == request.RatingType
                        && (limits.FromRating <= 0 || rpr.Rating >= limits.FromRating)
                        && (limits.ToRating <= 0 || rpr.Rating <= limits.ToRating)
                        && (limits.FromExp2Win <= 0 || rr.ExpectationToWin >= limits.FromExp2Win)
                        && (limits.ToExp2Win <= 0 || rr.ExpectationToWin <= limits.ToExp2Win)

                        && (!request.WithoutLeavers || rr.LeaverType == LeaverType.None)
                    group new { r, rp, rpr } by new { rp.Race, r.GameTime.Year, r.GameTime.Month } into g
                    select new TimelineEnt
                    {
                        Commander = g.Key.Race,
                        Time = new DateTime(g.Key.Year, g.Key.Month, 1),
                        Count = g.Count(),
                        AvgRating = Math.Round(g.Average(a => a.rpr.Rating), 2),
                        AvgGain = Math.Round(g.Average(a => a.rpr.Change), 2),
                        Wins = g.Count(s => s.rp.PlayerResult == PlayerResult.Win)
                    };

        var list = await group.ToListAsync(token);

        if (request.RatingType == RatingType.Cmdr || request.RatingType == RatingType.CmdrTE)
        {
            return list.Where(x => (int)x.Commander > 3).ToList();
        }
        else
        {
            return list.Where(x => x.Commander != Commander.None && (int)x.Commander <= 3).ToList();
        }
    }
}
