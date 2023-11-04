using dsstats.db8;
using dsstats.shared;
using dsstats.shared.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace dsstats.db8services;

public class SynergyService : ISynergyService
{
    private readonly ReplayContext context;
    private readonly IMemoryCache memoryCache;

    public SynergyService(ReplayContext context, IMemoryCache memoryCache)
    {
        this.context = context;
        this.memoryCache = memoryCache;
    }

    public async Task<SynergyResponse> GetSynergy(StatsRequest request, CancellationToken token = default)
    {
        var memKey = request.GenMemKey("Synergy");

        if (!memoryCache.TryGetValue(memKey, out SynergyResponse? response)
            || response is null)
        {
            response = await ProduceSynergy(request, token);
            memoryCache.Set(memKey, response, TimeSpan.FromHours(24));
        }
        return response;
    }

    private async Task<SynergyResponse> ProduceSynergy(StatsRequest request, CancellationToken token)
    {
        var data = request.ComboRating ?
            await GetComboSynergyData(request, token)
            : await GetSynergyData(request, token);

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
        if (ents.Count == 0)
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
            ent.NormalizedAvgGain = Math.Round(Constraint(ent.AvgGain, 0.0, 1.0, min, max), 2);
        }
    }

    private static double Constraint(double value, double minRange, double maxRange,
                                       double minVal, double maxVal)
    {
        return (((value - minVal) / (maxVal - minVal)) *
                  (maxRange - minRange) + minRange);
    }

    private async Task<List<SynergyEnt>> GetSynergyData(StatsRequest request, CancellationToken token)
    {
        (var fromDate, var toDate) = request.GetTimeLimits();
        var tillDate = toDate.AddDays(-2);

        var limits = request.GetFilterLimits();

        var query = from r in context.Replays
                    from rp1 in r.ReplayPlayers
                    from rp2 in r.ReplayPlayers
                    join rr in context.ReplayRatings on r.ReplayId equals rr.ReplayId
                    join rpr1 in context.RepPlayerRatings on rp1.ReplayPlayerId equals rpr1.ReplayPlayerId
                    join rpr2 in context.RepPlayerRatings on rp2.ReplayPlayerId equals rpr2.ReplayPlayerId
                    where r.GameTime > fromDate
                     && (toDate > tillDate || r.GameTime <= toDate)
                     && rr.RatingType == request.RatingType
                     && (limits.FromRating <= 0 || rpr1.Rating >= limits.FromRating)
                     && (limits.ToRating <= 0 || rpr1.Rating <= limits.ToRating)
                     && (limits.FromExp2Win <= 0 || rr.ExpectationToWin >= limits.FromExp2Win)
                     && (limits.ToExp2Win <= 0 || rr.ExpectationToWin <= limits.ToExp2Win)
                     && (!request.WithoutLeavers || rr.LeaverType == LeaverType.None)
                     && rp1.Team == rp2.Team
                     && rp1 != rp2
                    group new { r, rp1, rp2, rpr1, rpr2 } by new { rp1.Race, TeamRace = rp2.Race } into g
                    select new SynergyEnt()
                    {
                        Commander = g.Key.Race,
                        Teammate = g.Key.TeamRace,
                        Count = g.Count(),
                        Wins = g.Sum(c => c.rp1.PlayerResult == PlayerResult.Win ? 1 : 0),
                        AvgRating = Math.Round(g.Average(a => (a.rpr1.Rating + a.rpr2.Rating) / 2), 2),
                        AvgGain = Math.Round(g.Average(a => a.rpr1.RatingChange + a.rpr2.RatingChange), 2)
                    };

        return await query.ToListAsync(token);
    }

    private async Task<List<SynergyEnt>> GetComboSynergyData(StatsRequest request, CancellationToken token)
    {
        (var fromDate, var toDate) = request.GetTimeLimits();
        var tillDate = toDate.AddDays(-2);

        var limits = request.GetFilterLimits();

        var query = from r in context.Replays
                    from rp1 in r.ReplayPlayers
                    from rp2 in r.ReplayPlayers
                    join rr in context.ComboReplayRatings on r.ReplayId equals rr.ReplayId
                    join rpr1 in context.ComboReplayPlayerRatings on rp1.ReplayPlayerId equals rpr1.ReplayPlayerId
                    join rpr2 in context.ComboReplayPlayerRatings on rp2.ReplayPlayerId equals rpr2.ReplayPlayerId
                    where r.GameTime > fromDate
                     && (toDate > tillDate || r.GameTime <= toDate)
                     && rr.RatingType == request.RatingType
                     && (limits.FromRating <= 0 || rpr1.Rating >= limits.FromRating)
                     && (limits.ToRating <= 0 || rpr1.Rating <= limits.ToRating)
                     && (limits.FromExp2Win <= 0 || rr.ExpectationToWin >= limits.FromExp2Win)
                     && (limits.ToExp2Win <= 0 || rr.ExpectationToWin <= limits.ToExp2Win)
                     && (!request.WithoutLeavers || rr.LeaverType == LeaverType.None)
                     && rp1.Team == rp2.Team
                     && rp1 != rp2
                    group new { r, rp1, rp2, rpr1, rpr2 } by new { rp1.Race, TeamRace = rp2.Race } into g
                    select new SynergyEnt()
                    {
                        Commander = g.Key.Race,
                        Teammate = g.Key.TeamRace,
                        Count = g.Count(),
                        Wins = g.Sum(c => c.rp1.PlayerResult == PlayerResult.Win ? 1 : 0),
                        AvgRating = Math.Round(g.Average(a => (a.rpr1.Rating + a.rpr2.Rating) / 2), 2),
                        AvgGain = Math.Round(g.Average(a => (a.rpr1.Change + a.rpr2.Change) / 2), 2)
                    };

        return await query.ToListAsync(token);
    }
}
