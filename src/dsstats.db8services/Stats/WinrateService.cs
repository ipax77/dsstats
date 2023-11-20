
using dsstats.db8;
using dsstats.shared;
using dsstats.shared.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace dsstats.db8services;

public partial class WinrateService : IWinrateService
{
    private readonly IServiceScopeFactory scopeFactory;
    private readonly IMemoryCache memoryCache;
    private readonly ILogger<WinrateService> logger;

    public WinrateService(IServiceScopeFactory scopeFactory, IMemoryCache memoryCache, ILogger<WinrateService> logger)
    {
        this.scopeFactory = scopeFactory;
        this.memoryCache = memoryCache;
        this.logger = logger;
    }

    public async Task<WinrateResponse> GetWinrate(StatsRequest request, CancellationToken token)
    {
        var memKey = request.GenMemKey("Winrate");

        if (!memoryCache.TryGetValue(memKey, out WinrateResponse? response)
            || response is null)
        {
            try
            {
                response = await ProduceWinrate(request, token);
                if (response is not null)
                {
                    memoryCache.Set(memKey, response, TimeSpan.FromHours(3));
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                logger.LogError("failed producing winrate: {error}", ex.Message);
            }
        }
        return response ?? new();
    }

    public async Task<WinrateResponse> GetWinrate(WinrateRequest request, CancellationToken token)
    {
        var memKey = request.GenMemKey("Winrate");

        if (!memoryCache.TryGetValue(memKey, out WinrateResponse? response)
            || response is null)
        {
            try
            {
                response = await ProduceWinrate(request, token);
                if (response is not null)
                {
                    memoryCache.Set(memKey, response, TimeSpan.FromHours(3));
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                logger.LogError("failed producing winrate: {error}", ex.Message);
            }
        }
        return response ?? new();
    }

    private async Task<WinrateResponse?> ProduceWinrate(StatsRequest request, CancellationToken token)
    {
        var data = request.ComboRating ?
            await GetComboData(request, token)
            : await GetData(request, token);

        if (data is null)
        {
            return null;
        }

        if (request.RatingType == RatingType.Std || request.RatingType == RatingType.StdTE)
        {
            data = data.Where(x => x.Commander != Commander.None && (int)x.Commander <= 3).ToList();
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

    private async Task<List<WinrateEnt>?> GetData(StatsRequest request, CancellationToken token)
    {
        (var fromDate, var toDate) = request.GetTimeLimits();
        var tillDate = toDate.AddDays(-2);

        var limits = request.GetFilterLimits();

        using var scope = scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var group = request.Interest == Commander.None ?
                    from r in context.Replays
                    from rp in r.ReplayPlayers
                    join rr in context.ReplayRatings on r.ReplayId equals rr.ReplayId
                    join rpr in context.RepPlayerRatings on rp.ReplayPlayerId equals rpr.ReplayPlayerId
                    where r.GameTime > fromDate
                     && (toDate > tillDate || r.GameTime <= toDate)
                     && rr.RatingType == request.RatingType
                     && (limits.FromRating <= 0 || rpr.Rating >= limits.FromRating)
                     && (limits.ToRating <= 0 || rpr.Rating <= limits.ToRating)
                     && (limits.FromExp2Win <= 0 || rr.ExpectationToWin >= limits.FromExp2Win)
                     && (limits.ToExp2Win <= 0 || rr.ExpectationToWin <= limits.ToExp2Win)
                     && (!request.WithoutLeavers || rr.LeaverType == LeaverType.None)
                    group new { rp, rr, rpr, r } by rp.Race into g
                    select new WinrateEnt()
                    {
                        Commander = g.Key,
                        Count = g.Count(),
                        AvgRating = Math.Round(g.Average(a => a.rpr.Rating), 2),
                        AvgGain = Math.Round(g.Average(a => a.rpr.RatingChange), 2),
                        Wins = g.Sum(s => s.rp.PlayerResult == PlayerResult.Win ? 1 : 0),
                        Replays = g.Select(s => s.r.ReplayId).Distinct().Count()
                    }
                    :
                    from r in context.Replays
                    from rp in r.ReplayPlayers
                    join rr in context.ReplayRatings on r.ReplayId equals rr.ReplayId
                    join rpr in context.RepPlayerRatings on rp.ReplayPlayerId equals rpr.ReplayPlayerId
                    where r.GameTime > fromDate
                     && (toDate > tillDate || r.GameTime <= toDate)
                     && rr.RatingType == request.RatingType
                     && (limits.FromRating <= 0 || rpr.Rating >= limits.FromRating)
                     && (limits.ToRating <= 0 || rpr.Rating <= limits.ToRating)
                     && (limits.FromExp2Win <= 0 || rr.ExpectationToWin >= limits.FromExp2Win)
                     && (limits.ToExp2Win <= 0 || rr.ExpectationToWin <= limits.ToExp2Win)
                     && rp.Race == request.Interest
                     && (!request.WithoutLeavers || rr.LeaverType == LeaverType.None)
                    group new { rp, rr, rpr, r } by rp.OppRace into g
                    select new WinrateEnt()
                    {
                        Commander = g.Key,
                        Count = g.Count(),
                        AvgRating = Math.Round(g.Average(a => a.rpr.Rating), 2),
                        AvgGain = Math.Round(g.Average(a => a.rpr.RatingChange), 2),
                        Wins = g.Sum(s => s.rp.PlayerResult == PlayerResult.Win ? 1 : 0),
                        Replays = g.Select(s => s.r.ReplayId).Distinct().Count()
                    };

        var list = await group.ToListAsync();

        if (request.RatingType == RatingType.Cmdr || request.RatingType == RatingType.CmdrTE)
        {
            return list.Where(x => (int)x.Commander > 3).ToList();
        }
        else
        {
            return list.Where(x => x.Commander != Commander.None && (int)x.Commander <= 3).ToList();
        }
    }

    private async Task<List<WinrateEnt>?> GetComboData(StatsRequest request, CancellationToken token)
    {
        (var fromDate, var toDate) = request.GetTimeLimits();
        var tillDate = toDate.AddDays(-2);

        var limits = request.GetFilterLimits();

        using var scope = scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var group = request.Interest == Commander.None ?
                    from r in context.Replays
                    from rp in r.ReplayPlayers
                    join rr in context.ComboReplayRatings on r.ReplayId equals rr.ReplayId
                    join rpr in context.ComboReplayPlayerRatings on rp.ReplayPlayerId equals rpr.ReplayPlayerId
                    where r.GameTime > fromDate
                     && (toDate > tillDate || r.GameTime <= toDate)
                     && r.Duration > 300
                     && rr.RatingType == request.RatingType
                     && (limits.FromRating <= 0 || rpr.Rating >= limits.FromRating)
                     && (limits.ToRating <= 0 || rpr.Rating <= limits.ToRating)
                     && (limits.FromExp2Win <= 0 || rr.ExpectationToWin >= limits.FromExp2Win)
                     && (limits.ToExp2Win <= 0 || rr.ExpectationToWin <= limits.ToExp2Win)
                     && (!request.WithoutLeavers || rr.LeaverType == LeaverType.None)
                    group new { rp, rr, rpr, r } by rp.Race into g
                    select new WinrateEnt()
                    {
                        Commander = g.Key,
                        Count = g.Count(),
                        AvgRating = Math.Round(g.Average(a => a.rpr.Rating), 2),
                        AvgGain = Math.Round(g.Average(a => a.rpr.Change), 2),
                        Wins = g.Sum(s => s.rp.PlayerResult == PlayerResult.Win ? 1 : 0),
                        Replays = g.Select(s => s.r.ReplayId).Distinct().Count()
                    }
                    :
                    from r in context.Replays
                    from rp in r.ReplayPlayers
                    join rr in context.ComboReplayRatings on r.ReplayId equals rr.ReplayId
                    join rpr in context.ComboReplayPlayerRatings on rp.ReplayPlayerId equals rpr.ReplayPlayerId
                    where r.GameTime > fromDate
                     && (toDate > tillDate || r.GameTime <= toDate)
                     && r.Duration > 300
                     && rr.RatingType == request.RatingType
                     && (limits.FromRating <= 0 || rpr.Rating >= limits.FromRating)
                     && (limits.ToRating <= 0 || rpr.Rating <= limits.ToRating)
                     && (limits.FromExp2Win <= 0 || rr.ExpectationToWin >= limits.FromExp2Win)
                     && (limits.ToExp2Win <= 0 || rr.ExpectationToWin <= limits.ToExp2Win)
                     && rp.Race == request.Interest
                     && (!request.WithoutLeavers || rr.LeaverType == LeaverType.None)
                    group new { rp, rr, rpr, r } by rp.OppRace into g
                    select new WinrateEnt()
                    {
                        Commander = g.Key,
                        Count = g.Count(),
                        AvgRating = Math.Round(g.Average(a => a.rpr.Rating), 2),
                        AvgGain = Math.Round(g.Average(a => a.rpr.Change), 2),
                        Wins = g.Sum(s => s.rp.PlayerResult == PlayerResult.Win ? 1 : 0),
                        Replays = g.Select(s => s.r.ReplayId).Distinct().Count()
                    };

        var list = await group.ToListAsync();

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

