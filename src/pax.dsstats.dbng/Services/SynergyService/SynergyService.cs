using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using pax.dsstats.dbng.Extensions;
using pax.dsstats.shared;
using pax.dsstats.shared.Interfaces;
using System.Globalization;

namespace pax.dsstats.dbng.Services;

public class SynergyService : ISynergyService
{
    private readonly ReplayContext context;
    private readonly IMemoryCache memoryCache;

    public SynergyService(ReplayContext context, IMemoryCache memoryCache)
    {
        this.context = context;
        this.memoryCache = memoryCache;
    }

    public async Task<SynergyResponse> GetSynergy(SynergyRequest request, CancellationToken token = default)
    {
        var memKey = request.GenMemKey();

        if (!memoryCache.TryGetValue(memKey, out SynergyResponse response))
        {
            response = await ProduceSynergy(request, token);
            memoryCache.Set(memKey, response, TimeSpan.FromHours(24));
        }
        return response;
    }

    public async Task<SynergyResponse> ProduceSynergy(SynergyRequest request, CancellationToken token = default)
    {
        var data = await GetData(request, token);

        if (request.RatingType == RatingType.Std || request.RatingType == RatingType.StdTE)
        {
            data = data.Where(x => (int)x.Commander <= 3 && (int)x.Teammate <= 3).ToList();
        }
        else
        {
            data = data.Where(x => (int)x.Commander > 3 && (int)x.Teammate > 3).ToList();
        }

        Normalize(data);

        return new()
        {
            Entities = data,
        };
    }

    private void Normalize(List<SynergyEnt> ents)
    {
        if (!ents.Any())
        {
            return;
        }

        var min = ents.Min(m => m.AvgGain);
        var max = ents.Max(m => m.AvgGain);

        var range = max - min;
        if (range == 0)
        {
            return;
        }
        
        foreach (var ent in ents)
        {
            ent.NormalizedAvgGain = Math.Round((ent.AvgGain - min) / range, 2);
        }
    }

    private async Task<List<SynergyEnt>> GetData(SynergyRequest request, CancellationToken token)
    {
        (var fromDate, var toDate) = Data.TimeperiodSelected(request.TimePeriod);

        var sql =
            $@"
                SELECT rp1.Race as commander,
                  rp2.Race as teammate,
                count(*) as count,
                  round(avg(rpr1.Rating), 2) as avgrating,
                  round(avg(rpr1.RatingChange), 2) as avggain,
                  sum(CASE WHEN rp1.PlayerResult = 1 THEN 1 ELSE 0 END) as wins,
                  0.0 as normalizedAvgGain
                FROM Replays as r
                INNER JOIN ReplayRatings as rr on rr.ReplayId = r.ReplayId
                INNER JOIN ReplayPlayers AS rp1 on rp1.ReplayId = r.ReplayId
                INNER JOIN ReplayPlayers AS rp2 on rp2.ReplayId = r.ReplayId
                INNER JOIN RepPlayerRatings AS rpr1 on rpr1.ReplayPlayerId = rp1.ReplayPlayerId
                INNER JOIN RepPlayerRatings AS rpr2 on rpr2.ReplayPlayerId = rp2.ReplayPlayerId
                WHERE rr.RatingType = {(int)request.RatingType}
                AND r.GameTime > '{fromDate.ToString("yyyy-MM-dd")}'
                {(toDate < DateTime.Today.AddDays(-2) ? $"AND r.GameTime < '{toDate.ToString("yyyy-MM-dd")}'" : "")}
                {(request.WithLeavers ? "" : "AND rr.LeaverType = 0")}
                {(request.FromRating > Data.MinBuildRating ? $"AND rpr1.Rating >= {request.FromRating}" : "")}
                {(request.ToRating != 0 && request.ToRating < Data.MaxBuildRating ? $"AND rpr1.Rating <= {request.ToRating}" : "")}
                {(request.Exp2WinOffset != 0 ? $"AND rr.ExpectationToWin >= {((50 - request.Exp2WinOffset) / 100.0).ToString(CultureInfo.InvariantCulture)} AND rr.ExpectationToWin <= {((50 + request.Exp2WinOffset) / 100.0).ToString(CultureInfo.InvariantCulture)}" : "")}
                AND rp2.Team = rp1.Team
                AND rp2.ReplayPlayerId != rp1.ReplayPlayerId
                GROUP BY rp1.Race, rp2.Race;
            ";

        var result = await context.SynergyEnts
            .FromSqlRaw(sql)
            .ToListAsync(token);
        return result;
    }
}

