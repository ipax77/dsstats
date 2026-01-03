using dsstats.db;
using dsstats.shared;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore;
using dsstats.shared.Interfaces;
using dsstats.shared.Units;

namespace dsstats.dbServices.Builds;

public partial class BuildsService(DsstatsContext context, IMemoryCache memoryCache) : IBuildsService
{
    public async Task<BuildsResponse> GetBuildResponse(BuildsRequest request, CancellationToken token = default)
    {
        var memKey = request.GetMemKey();
        try
        {
            return await memoryCache.GetOrCreateAsync(memKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24);
                var response = await CreateBuildsResponse(request, token);
                await SetBuildResponseLifeAndCost(response, request.Interest);
                if (request.WithSpawnInfo)
                {
                    response.Replays = await GetBuildReplays(request);
                }
                return response;
            }) ?? new();
        }
        catch (OperationCanceledException)
        {
            return new BuildsResponse();
        }
    }

    private async Task<BuildsResponse> CreateBuildsResponse(BuildsRequest request, CancellationToken token)
    {
        if (request.Players.Count > 0)
        {
            return await CreatePlayerBuildsResponse(request, token);
        }

        var timeInfo = Data.GetTimePeriodInfo(request.TimePeriod);
        if (timeInfo is null)
        {
            return new();
        }

        var unitsquery = from rp in context.ReplayPlayers
                         join rr in context.ReplayRatings on rp.ReplayId equals rr.ReplayId
                         join cpr in context.ReplayPlayerRatings on rp.ReplayPlayerId equals cpr.ReplayPlayerId
                         from sp in rp.Spawns
                         from su in sp.Units
                         where rp.Race == request.Interest
                          && rp.Replay!.Gametime >= timeInfo.Start
                          && (!timeInfo.HasEnd || rp.Replay!.Gametime < timeInfo.End)
                          && rr.LeaverType == LeaverType.None
                          && rr.RatingType == request.RatingType
                          && sp.Breakpoint == request.Breakpoint
                          && (request.Versus == Commander.None || rp.OppRace == request.Versus)
                          && (request.FromRating <= Data.MinBuildRating || cpr.RatingBefore > request.FromRating)
                          && (request.ToRating >= Data.MaxBuildRating || cpr.RatingBefore < request.ToRating)
                         group su by new { su.UnitId, su.Unit!.Name } into g
                         select new
                         {
                             g.Key.UnitId,
                             g.Key.Name,
                             UnitCount = g.Sum(s => s.Count),
                         };
        var result = await unitsquery
            .OrderByDescending(o => o.UnitCount)
            .ToListAsync(token);

        var buildStats = await CreateBuildStats(request, timeInfo, token);

        var response = new BuildsResponse()
        {
            Stats = buildStats,
            Units = result.Select(s => new BuildUnit()
            {
                Name = s.Name,
                Count = buildStats.CmdrCount == 0 ? s.UnitCount : Math.Round(s.UnitCount / (double)buildStats.CmdrCount, 2)
            }).ToList()
        };
        return response;
    }

    private async Task<BuildStats> CreateBuildStats(BuildsRequest request, TimePeriodInfo timeInfo, CancellationToken token)
    {
        var query = from rp in context.ReplayPlayers
                    join sp in context.Spawns on rp.ReplayPlayerId equals sp.ReplayPlayerId
                    join r in context.Replays on rp.ReplayId equals r.ReplayId
                    join rr in context.ReplayRatings on r.ReplayId equals rr.ReplayId
                    join rpr in context.ReplayPlayerRatings on rp.ReplayPlayerId equals rpr.ReplayPlayerId
                    where rr.LeaverType == LeaverType.None
                    && rr.RatingType == request.RatingType
                    && r.Gametime >= timeInfo.Start
                    && (!timeInfo.HasEnd || r.Gametime < timeInfo.End)
                    && rp.Race == request.Interest
                    && (request.Versus == Commander.None || rp.OppRace == request.Versus)
                    && (request.FromRating <= Data.MinBuildRating || rpr.RatingBefore >= request.FromRating)
                    && (request.ToRating >= Data.MaxBuildRating || rpr.RatingBefore <= request.ToRating)
                    && sp.Breakpoint == request.Breakpoint
                    group new { r, rp, rpr, sp } by sp.Breakpoint into g
                    select new BuildStats()
                    {
                        Count = g.Select(s => s.r.ReplayId).Distinct().Count(),
                        CmdrCount = g.Count(),
                        Winrate = Math.Round(100.0 * g.Sum(x => x.rp.Result == PlayerResult.Win ? 1 : 0) / g.Count(), 2),
                        AvgGain = Math.Round(g.Average(x => x.rpr.RatingDelta), 2),
                        Duration = Math.Round(g.Average(x => x.r.Duration), 2),
                        Gas = Math.Round(g.Average(x => x.sp.GasCount), 2),
                        Upgrades = Math.Round(g.Average(x => x.sp.UpgradeSpent), 2)
                    };

        var buildCounts = await query.FirstOrDefaultAsync(token);
        return buildCounts ?? new();
    }

    private async Task<BuildsResponse> CreatePlayerBuildsResponse(BuildsRequest request, CancellationToken token)
    {
        var timeInfo = Data.GetTimePeriodInfo(request.TimePeriod);
        if (timeInfo is null)
        {
            return new();
        }

        HashSet<int> playerIds = request.Players.Select(s => s.PlayerId).ToHashSet();

        var unitsquery = from rp in context.ReplayPlayers
                         join rr in context.ReplayRatings on rp.ReplayId equals rr.ReplayId
                         join cpr in context.ReplayPlayerRatings on rp.ReplayPlayerId equals cpr.ReplayPlayerId
                         from sp in rp.Spawns
                         from su in sp.Units
                         where rp.Race == request.Interest
                          && rp.Replay!.Gametime >= timeInfo.Start
                          && (!timeInfo.HasEnd || rp.Replay!.Gametime < timeInfo.End)
                          && rr.LeaverType == LeaverType.None
                          && rr.RatingType == request.RatingType
                          && sp.Breakpoint == request.Breakpoint
                          && (request.Versus == Commander.None || rp.OppRace == request.Versus)
                          && playerIds.Contains(rp.PlayerId)
                         group su by new { su.UnitId, su.Unit!.Name } into g
                         select new
                         {
                             g.Key.UnitId,
                             g.Key.Name,
                             UnitCount = g.Sum(s => s.Count),
                         };
        var rawUnits = await unitsquery
            .OrderByDescending(o => o.UnitCount)
            .ToListAsync(token);

        var buildStats = await CreatePlayerBuildStats(request, timeInfo, token);

        var normalizedUnits = rawUnits
            .Select(u => new
            {
                NormalizedName = UnitMap.GetNormalizedUnitName(u.Name, request.Interest),
                u.UnitCount
            })
            .GroupBy(u => u.NormalizedName)
            .Select(g => new
            {
                Name = g.Key,
                UnitCount = g.Sum(x => x.UnitCount)
            })
            .OrderByDescending(u => u.UnitCount)
            .ToList();

        var response = new BuildsResponse
        {
            Stats = buildStats,
            Units = normalizedUnits.Select(u => new BuildUnit
            {
                Name = u.Name,
                Count = buildStats.CmdrCount == 0
                    ? u.UnitCount
                    : Math.Round(u.UnitCount / (double)buildStats.CmdrCount, 2)
            }).ToList()
        };

        return response;
    }

    private async Task<BuildStats> CreatePlayerBuildStats(BuildsRequest request, TimePeriodInfo timeInfo, CancellationToken token)
    {
        HashSet<int> playerIds = request.Players.Select(s => s.PlayerId).ToHashSet();

        var query = from rp in context.ReplayPlayers
                    join sp in context.Spawns on rp.ReplayPlayerId equals sp.ReplayPlayerId
                    join r in context.Replays on rp.ReplayId equals r.ReplayId
                    join rr in context.ReplayRatings on r.ReplayId equals rr.ReplayId
                    join rpr in context.ReplayPlayerRatings on rp.ReplayPlayerId equals rpr.ReplayPlayerId
                    where rr.LeaverType == LeaverType.None
                    && rr.RatingType == request.RatingType
                    && r.Gametime >= timeInfo.Start
                    && (!timeInfo.HasEnd || r.Gametime < timeInfo.End)
                    && rp.Race == request.Interest
                    && (request.Versus == Commander.None || rp.OppRace == request.Versus)
                    && sp.Breakpoint == request.Breakpoint
                    && playerIds.Contains(rp.PlayerId)
                    group new { r, rp, rpr, sp } by sp.Breakpoint into g
                    select new BuildStats()
                    {
                        Count = g.Select(s => s.r.ReplayId).Distinct().Count(),
                        CmdrCount = g.Count(),
                        Winrate = Math.Round(100.0 * g.Sum(x => x.rp.Result == PlayerResult.Win ? 1 : 0) / g.Count(), 2),
                        AvgGain = Math.Round(g.Average(x => x.rpr.RatingDelta), 2),
                        Duration = Math.Round(g.Average(x => x.r.Duration), 2),
                        Gas = Math.Round(g.Average(x => x.sp.GasCount), 2),
                        Upgrades = Math.Round(g.Average(x => x.sp.UpgradeSpent), 2)
                    };

        var buildCounts = await query.FirstOrDefaultAsync(token);
        return buildCounts ?? new();
    }
}
