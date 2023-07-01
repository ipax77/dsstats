using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using pax.dsstats.dbng.Extensions;
using pax.dsstats.shared;
using pax.dsstats.shared.Interfaces;

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

        return new()
        {
            Entities = data,
        };
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
                  sum(CASE WHEN rp1.PlayerResult = 1 THEN 1 ELSE 0 END) as wins
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

