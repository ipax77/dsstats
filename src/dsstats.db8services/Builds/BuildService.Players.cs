
using dsstats.db8;
using dsstats.shared;
using LinqKit;
using Microsoft.EntityFrameworkCore;

namespace dsstats.db8services;

public partial class BuildService
{
    public async Task<BuildResponse> ProducePlayerBuilds(BuildRequest request, CancellationToken token)
    {
        (var start, var end) = Data.TimeperiodSelected(request.TimePeriod);
        bool noEnd = end >= DateTime.Today.AddDays(-2);
        var ratingTypes = GetRatingTypes(request);

        var query = from r in context.Replays
                    from rp in r.ReplayPlayers
                    from sp in rp.Spawns
                    from su in sp.Units
                    join rr in context.ReplayRatings on r.ReplayId equals rr.ReplayId
                    join rpr in context.ComboReplayPlayerRatings on rp.ReplayPlayerId equals rpr.ReplayPlayerId
                    where r.GameTime >= start
                     && (noEnd || r.GameTime < end)
                     && rp.Race == request.Interest
                     && (request.Versus == Commander.None || rp.OppRace == request.Versus)
                     && rr.LeaverType == LeaverType.None
                     && ratingTypes.Contains(rr.RatingType)
                     && sp.Breakpoint == request.Breakpoint
                    select new PlayerUnitGroup()
                    {
                        p = rp.Player,
                        su = su
                    };

        var predicate = PredicateBuilder.New<PlayerUnitGroup>();

        foreach (var player in request.PlayerNames)
        {
            predicate = predicate.Or(o => o.p.ToonId == player.ToonId
                && o.p.RealmId == player.RealmId
                && o.p.RegionId == player.RegionId);
        }

        query = query.Where(predicate);

        var unitsquery = from a in query
                         group a.su by new { a.su.UnitId, a.su.Unit.Name } into g
                         select new
                         {
                             g.Key.UnitId,
                             g.Key.Name,
                             UnitCount = g.Sum(s => s.Count),
                         };

        var result = await unitsquery
            .OrderByDescending(o => o.UnitCount)
            .ToListAsync(token);
        var buildCounts = await GetPlayersCountResult(request, token);

        return new()
        {
            BuildCounts = buildCounts,
            Units = result.Select(s => new BuildResponseBreakpointUnit()
            {
                Name = s.Name,
                Count = buildCounts.CmdrCount == 0 ? s.UnitCount : Math.Round(s.UnitCount / (double)buildCounts.CmdrCount, 2)
            }).ToList()
        };
    }

    public record PlayerUnitGroup
    {
        public Player p { get; set; } = null!;
        public SpawnUnit su { get; set; } = null!;
    }

    private async Task<BuildCounts> GetPlayersCountResult(BuildRequest request, CancellationToken token)
    {
        if (IsSqlite)
        {
            return await GetMauiPlayersCountResult(request, token);
        }

        (var start, var end) = Data.TimeperiodSelected(request.TimePeriod);
        bool noEnd = end >= DateTime.Today.AddDays(-2);
        var ratingTypes = GetRatingTypes(request);

        var rawgroup = from rp in context.ReplayPlayers
                       join p in context.Players on rp.PlayerId equals p.PlayerId
                       join r in context.Replays on rp.ReplayId equals r.ReplayId
                       join rr in context.ReplayRatings on r.ReplayId equals rr.ReplayId
                       join rpr in context.ComboReplayPlayerRatings on rp.ReplayPlayerId equals rpr.ReplayPlayerId
                       from sp in rp.Spawns
                       where r.GameTime >= start
                        && (noEnd || r.GameTime <= end)
                        && rr.LeaverType == LeaverType.None
                        && ratingTypes.Contains(rr.RatingType)
                        && rp.Race == request.Interest
                        && (request.Versus == Commander.None || rp.OppRace == request.Versus)
                        && sp.Breakpoint == request.Breakpoint
                       select new CountGroup()
                       {
                           r = r,
                           rp = rp,
                           rpr = rpr,
                           p = p,
                           sp = sp
                       };

        var predicate = PredicateBuilder.New<CountGroup>();

        foreach (var pl in request.PlayerNames)
        {
            predicate = predicate.Or(o => o.p.ToonId == pl.ToonId && o.p.RealmId == pl.RealmId && o.p.RegionId == pl.RegionId);
        }
        rawgroup = rawgroup.Where(predicate);

        var filteredGroup = from a in rawgroup
                            group a by a.sp.Breakpoint into g
                            select new BuildCounts()
                            {
                                Count = g.Select(s => s.r.ReplayId).Distinct().Count(),
                                Winrate = Math.Round(100.0 * g.Sum(x => x.rp.PlayerResult == PlayerResult.Win ? 1 : 0) / g.Count(), 2),
                                AvgGain = Math.Round(g.Average(x => x.rpr.Change), 2),
                                Duration = Math.Round(g.Average(x => x.r.Duration), 2),
                                Gas = Math.Round(g.Average(x => x.sp.GasCount), 2),
                                Upgrades = Math.Round(g.Average(x => x.sp.UpgradeSpent), 2)
                            };


        var buildCount = await filteredGroup.FirstOrDefaultAsync(token);
        if (buildCount is not null)
        {
            buildCount = buildCount with { CmdrCount = buildCount.Count };
        }
        return buildCount ?? new();
    }

    private async Task<BuildCounts> GetMauiPlayersCountResult(BuildRequest request, CancellationToken token)
    {
        (var start, var end) = Data.TimeperiodSelected(request.TimePeriod);
        bool noEnd = end >= DateTime.Today.AddDays(-2);
        var ratingTypes = GetRatingTypes(request);

        var rawgroup = from rp in context.ReplayPlayers
                       join p in context.Players on rp.PlayerId equals p.PlayerId
                       join r in context.Replays on rp.ReplayId equals r.ReplayId
                       join rr in context.ReplayRatings on r.ReplayId equals rr.ReplayId
                       join rpr in context.RepPlayerRatings on rp.ReplayPlayerId equals rpr.ReplayPlayerId
                       from sp in rp.Spawns
                       where r.GameTime >= start
                        && (noEnd || r.GameTime <= end)
                        && rr.LeaverType == LeaverType.None
                        && ratingTypes.Contains(rr.RatingType)
                        && rp.Race == request.Interest
                        && (request.Versus == Commander.None || rp.OppRace == request.Versus)
                        && sp.Breakpoint == request.Breakpoint
                       select new MauiCountGroup()
                       {
                           r = r,
                           rp = rp,
                           rpr = rpr,
                           p = p,
                           sp = sp
                       };

        var predicate = PredicateBuilder.New<MauiCountGroup>();

        foreach (var pl in request.PlayerNames)
        {
            predicate = predicate.Or(o => o.p.ToonId == pl.ToonId && o.p.RealmId == pl.RealmId && o.p.RegionId == pl.RegionId);
        }
        rawgroup = rawgroup.Where(predicate);

        var filteredGroup = from a in rawgroup
                            group a by a.sp.Breakpoint into g
                            select new BuildCounts()
                            {
                                Count = g.Select(s => s.r.ReplayId).Distinct().Count(),
                                Winrate = Math.Round(100.0 * g.Sum(x => x.rp.PlayerResult == PlayerResult.Win ? 1 : 0) / g.Count(), 2),
                                AvgGain = Math.Round(g.Average(x => x.rpr.RatingChange), 2),
                                Duration = Math.Round(g.Average(x => x.r.Duration), 2),
                                Gas = Math.Round(g.Average(x => x.sp.GasCount), 2),
                                Upgrades = Math.Round(g.Average(x => x.sp.UpgradeSpent), 2)
                            };


        var buildCount = await filteredGroup.FirstOrDefaultAsync(token);
        if (buildCount is not null)
        {
            buildCount = buildCount with { CmdrCount = buildCount.Count };
        }
        return buildCount ?? new();
    }

    public record CountGroup
    {
        public Replay r { get; set; } = null!;
        public ReplayPlayer rp { get; set; } = null!;
        public ComboReplayPlayerRating rpr { get; set; } = null!;
        public Player p { get; set; } = null!;
        public Spawn sp { get; set; } = null!;
    }

    public record MauiCountGroup
    {
        public Replay r { get; set; } = null!;
        public ReplayPlayer rp { get; set; } = null!;
        public RepPlayerRating rpr { get; set; } = null!;
        public Player p { get; set; } = null!;
        public Spawn sp { get; set; } = null!;
    }
}
