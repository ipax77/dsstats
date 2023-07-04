
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using pax.dsstats.dbng.Extensions;
using pax.dsstats.shared;
using pax.dsstats.shared.Interfaces;
using System.Globalization;

namespace pax.dsstats.dbng.Services;

public class DamageService : IDamageService
{
    private readonly ReplayContext context;
    private readonly IMemoryCache memoryCache;

    public DamageService(ReplayContext context, IMemoryCache memoryCache)
    {
        this.context = context;
        this.memoryCache = memoryCache;
    }

    public async Task<DamageResponse> GetDamage(DamageRequest request, CancellationToken token)
    {
        var memKey = request.GenMemKey();
        if (!memoryCache.TryGetValue(memKey, out DamageResponse response))
        {
            response = await ProduceDamage(request, token);
            memoryCache.Set(memKey, response, TimeSpan.FromHours(24));
        }
        return response;
    }

    public async Task<DamageResponse> ProduceDamage(DamageRequest request, CancellationToken token)
    {
        var data = await GetData(request, token);

        return new()
        {
            Entities = data
        };
    }

    private async Task<List<DamageEnt>> GetData(DamageRequest request, CancellationToken token)
    {
        (var fromDate, var toDate) = Data.TimeperiodSelected(request.TimePeriod);

        var sql =
            $@"
                select rp.Race as commander,
                s.Breakpoint,
                count(*) as count,
                sum(CASE WHEN rp.Kills = r.Maxkillsum THEN 1 ELSE 0 END) as mvp,
                round(avg(s.KilledValue)) as avgKills,
                round(avg(s.ArmyValue)) as avgArmy,
                round(avg(s.UpgradeSpent)) as avgUpgrades,
                round(avg(s.GasCount), 2) as avggas,
                round(avg(s.Income)) as avgincome,
                round(avg(rp.APM)) as avgapm
                from Replays as r
                inner join ReplayRatings as rr on rr.ReplayId = r.ReplayId
                inner join ReplayPlayers as rp on rp.ReplayId = r.ReplayId
                inner join Spawns as s on s.ReplayPlayerId = rp.ReplayPlayerId
                inner join RepPlayerRatings as rpr on rpr.ReplayPlayerId = rp.ReplayPlayerId
                where r.GameTime > '{fromDate.ToString("yyyy-MM-dd")}'
                    {(toDate < DateTime.Today.AddDays(-2) ? $"AND r.GameTime < '{toDate.ToString("yyyy-MM-dd")}'" : "")}
	                {(request.WithLeavers ? "" : "AND rr.LeaverType = 0")}
                    AND rr.RatingType = {(int)request.RatingType}
                    {(request.Interest == Commander.None ? "" : $"AND rp.OppRace = {(int)request.Interest}")}
                    {(request.FromRating > Data.MinBuildRating ? $"AND rpr.Rating >= {request.FromRating}" : "")}
                    {(request.ToRating != 0 && request.ToRating < Data.MaxBuildRating ? $"AND rpr.Rating <= {request.ToRating}" : "")}
                    {(request.Exp2WinOffset != 0 ? $"AND rr.ExpectationToWin >= {((50 - request.Exp2WinOffset) / 100.0).ToString(CultureInfo.InvariantCulture)} AND rr.ExpectationToWin <= {((50 + request.Exp2WinOffset) / 100.0).ToString(CultureInfo.InvariantCulture)}" : "")}
                group by rp.Race, s.Breakpoint;
            ";

        var result = await context.DamageEnts
            .FromSqlRaw(sql)
            .ToListAsync(token);
        return result;
    }
}

