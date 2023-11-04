using dsstats.db8;
using dsstats.shared;
using dsstats.shared.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace dsstats.db8services;

public class DamageService : IDamageService
{
    private readonly ReplayContext context;
    private readonly IMemoryCache memoryCache;

    public DamageService(ReplayContext context, IMemoryCache memoryCache)
    {
        this.context = context;
        this.memoryCache = memoryCache;
    }

    public async Task<DamageResponse> GetDamage(StatsRequest request, CancellationToken token)
    {
        var memKey = request.GenMemKey("Damage");
        if (!memoryCache.TryGetValue(memKey, out DamageResponse? response)
            || response is null)
        {
            response = await ProduceDamage(request, token);
            memoryCache.Set(memKey, response, TimeSpan.FromHours(24));
        }
        return response;
    }

    public async Task<DamageResponse> ProduceDamage(StatsRequest request, CancellationToken token)
    {
        var data = request.ComboRating ?
            await GetComboData(request, token)
            : await GetData(request, token);

        return new()
        {
            Entities = data
        };
    }

    private async Task<List<DamageEnt>> GetData(StatsRequest request, CancellationToken token)
    {
        (var fromDate, var toDate) = request.GetTimeLimits();
        var tillDate = DateTime.Today.AddDays(-2);

        var limits = request.GetFilterLimits();

        var query = from r in context.Replays
                    from rp in r.ReplayPlayers
                    join s in context.Spawns on rp.ReplayPlayerId equals s.ReplayPlayerId
                    join rr in context.ReplayRatings on r.ReplayId equals rr.ReplayId
                    join rpr in context.RepPlayerRatings on rp.ReplayPlayerId equals rpr.ReplayPlayerId
                    where r.GameTime >= fromDate
                     && (toDate > tillDate || r.GameTime < toDate)
                     && (request.Interest == Commander.None || rp.OppRace == request.Interest)
                     && rr.RatingType == request.RatingType
                     && (limits.FromRating <= 0 || rpr.Rating >= limits.FromRating)
                     && (limits.ToRating <= 0 || rpr.Rating <= limits.ToRating)
                     && (limits.FromExp2Win <= 0 || rr.ExpectationToWin >= limits.FromExp2Win)
                     && (limits.ToExp2Win <= 0 || rr.ExpectationToWin <= limits.ToExp2Win)
                     && (!request.WithoutLeavers || rr.LeaverType == LeaverType.None)
                    group new { r, rp, s } by new { rp.Race, s.Breakpoint } into g
                    select new DamageEnt()
                    {
                        Commander = g.Key.Race,
                        Breakpoint = g.Key.Breakpoint,
                        Count = g.Count(),
                        Mvp = g.Sum(s => s.rp.Kills == s.r.Maxkillsum ? 1 : 0),
                        AvgKills = (int)Math.Round(g.Average(a => a.s.KilledValue)),
                        AvgArmy = (int)Math.Round(g.Average(a => a.s.ArmyValue)),
                        AvgUpgrades = (int)Math.Round(g.Average(a => a.s.UpgradeSpent)),
                        AvgGas = Math.Round(g.Average(a => a.s.GasCount), 2),
                        AvgIncome = (int)Math.Round(g.Average(a => a.s.Income)),
                        AvgAPM = (int)Math.Round(g.Average(a => a.rp.APM))
                    };

        return await query.ToListAsync(token);
    }

    private async Task<List<DamageEnt>> GetComboData(StatsRequest request, CancellationToken token)
    {
        (var fromDate, var toDate) = request.GetTimeLimits();
        var tillDate = DateTime.Today.AddDays(-2);

        var limits = request.GetFilterLimits();

        var query = from r in context.Replays
                    from rp in r.ReplayPlayers
                    join s in context.Spawns on rp.ReplayPlayerId equals s.ReplayPlayerId
                    join rr in context.ReplayRatings on r.ReplayId equals rr.ReplayId
                    join rpr in context.ComboReplayPlayerRatings on rp.ReplayPlayerId equals rpr.ReplayPlayerId
                    where r.GameTime >= fromDate
                     && (toDate > tillDate || r.GameTime < toDate)
                     && (request.Interest == Commander.None || rp.OppRace == request.Interest)
                     && rr.RatingType == request.RatingType
                     && (limits.FromRating <= 0 || rpr.Rating >= limits.FromRating)
                     && (limits.ToRating <= 0 || rpr.Rating <= limits.ToRating)
                     && (limits.FromExp2Win <= 0 || rr.ExpectationToWin >= limits.FromExp2Win)
                     && (limits.ToExp2Win <= 0 || rr.ExpectationToWin <= limits.ToExp2Win)
                     && (!request.WithoutLeavers || rr.LeaverType == LeaverType.None)
                    group new { r, rp, s } by new { rp.Race, s.Breakpoint } into g
                    select new DamageEnt()
                    {
                        Commander = g.Key.Race,
                        Breakpoint = g.Key.Breakpoint,
                        Count = g.Count(),
                        Mvp = g.Sum(s => s.rp.Kills == s.r.Maxkillsum ? 1 : 0),
                        AvgKills = (int)Math.Round(g.Average(a => a.s.KilledValue)),
                        AvgArmy = (int)Math.Round(g.Average(a => a.s.ArmyValue)),
                        AvgUpgrades = (int)Math.Round(g.Average(a => a.s.UpgradeSpent)),
                        AvgGas = (int)Math.Round(g.Average(a => a.s.GasCount)),
                        AvgIncome = (int)Math.Round(g.Average(a => a.s.Income)),
                        AvgAPM = (int)Math.Round(g.Average(a => a.rp.APM))
                    };

        return await query.ToListAsync(token);
    }
}
