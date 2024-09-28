
using dsstats.shared;
using Microsoft.EntityFrameworkCore;

namespace dsstats.db8services;

public partial class BuildService
{


    private async Task<BuildResponse> ProduceBuild(BuildRequest request, CancellationToken token)
    {
        (var start, var end) = Data.TimeperiodSelected(request.TimePeriod);
        bool noEnd = end >= DateTime.Today.AddDays(-2);
        var ratingTypes = GetRatingTypes(request);

        var unitsquery = from rp in context.ReplayPlayers
                         join rr in context.ReplayRatings on rp.ReplayId equals rr.ReplayId
                         join cpr in context.ComboReplayPlayerRatings on rp.ReplayPlayerId equals cpr.ReplayPlayerId
                         from sp in rp.Spawns
                         from su in sp.Units
                         where rp.Race == request.Interest
                          && rp.Replay.GameTime >= start
                          && (noEnd || rp.Replay.GameTime < end)
                          && rr.LeaverType == LeaverType.None
                          && ratingTypes.Contains(rr.RatingType)
                          && sp.Breakpoint == request.Breakpoint
                          && (request.Versus == Commander.None || rp.OppRace == request.Versus)
                          && (request.FromRating <= Data.MinBuildRating || cpr.Rating > request.FromRating)
                          && (request.ToRating >= Data.MaxBuildRating || cpr.Rating < request.ToRating)
                         group su by new { su.UnitId, su.Unit.Name } into g
                         select new
                         {
                             g.Key.UnitId,
                             g.Key.Name,
                             UnitCount = g.Sum(s => s.Count),
                         };

        var result = await unitsquery
            .OrderByDescending(o => o.UnitCount)
            .ToListAsync(token);

        var buildCounts = await GetCountResult(request, token);

        return new()
        {
            BuildCounts = buildCounts,
            Units = result.Select(s => new BuildResponseBreakpointUnit()
            {
                Name = s.Name,
                // Count = buildCounts.Count == 0 ? s.UnitCount : Math.Round(s.UnitCount / (double)buildCounts.Count, 2)
                Count = buildCounts.CmdrCount == 0 ? s.UnitCount : Math.Round(s.UnitCount / (double)buildCounts.CmdrCount, 2)
            }).ToList()
        };
    }

    private List<RatingType> GetRatingTypes(BuildRequest request)
    {
        return request.RatingType switch
        {
            RatingType.Cmdr => new() { RatingType.Cmdr, RatingType.CmdrTE },
            RatingType.Std => new() { RatingType.Std, RatingType.StdTE },
            _ => new() { request.RatingType }
        };
    }

    private async Task<BuildCounts> GetCountResult(BuildRequest request, CancellationToken token)
    {
        (var start, var end) = Data.TimeperiodSelected(request.TimePeriod);
        bool noEnd = end >= DateTime.Today.AddDays(-2);
        var ratingTypes = GetRatingTypes(request);

        var query = from rp in context.ReplayPlayers
                    join sp in context.Spawns on rp.ReplayPlayerId equals sp.ReplayPlayerId
                    join r in context.Replays on rp.ReplayId equals r.ReplayId
                    join rr in context.ReplayRatings on r.ReplayId equals rr.ReplayId
                    join rpr in context.ComboReplayPlayerRatings on rp.ReplayPlayerId equals rpr.ReplayPlayerId
                    where rr.LeaverType == 0
                    && ratingTypes.Contains(rr.RatingType)
                    && r.GameTime >= start
                    && (noEnd || r.GameTime < end)
                    && rp.Race == request.Interest
                    && (request.Versus == Commander.None || rp.OppRace == request.Versus)
                    && (request.FromRating <= Data.MinBuildRating || rpr.Rating >= request.FromRating)
                    && (request.ToRating >= Data.MaxBuildRating || rpr.Rating <= request.ToRating)
                    && sp.Breakpoint == request.Breakpoint
                    group new { r, rp, rpr, sp } by sp.Breakpoint into g
                    select new BuildCounts()
                    {
                        Count = g.Select(s => s.r.ReplayId).Distinct().Count(),
                        CmdrCount = g.Count(),
                        Winrate = Math.Round(100.0 * g.Sum(x => x.rp.PlayerResult == PlayerResult.Win ? 1 : 0) / g.Count(), 2),
                        AvgGain = Math.Round(g.Average(x => x.rpr.Change), 2),
                        Duration = Math.Round(g.Average(x => x.r.Duration), 2),
                        Gas = Math.Round(g.Average(x => x.sp.GasCount), 2),
                        Upgrades = Math.Round(g.Average(x => x.sp.UpgradeSpent), 2)
                    };

        var buildCounts = await query.FirstOrDefaultAsync(token);
        return buildCounts ?? new BuildCounts();
    }
}