using dsstats.db;
using dsstats.shared;
using dsstats.shared.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Runtime.InteropServices;

namespace dsstats.dbServices.Builds;

public partial class BuildsService(IDbContextFactory<DsstatsContext> contextFactory, IMemoryCache memoryCache) : IBuildsService
{
    public async Task<BuildsResponse> GetBuildResponse(BuildsRequest request, CancellationToken token = default)
    {
        var memKey = request.GetMemKey();
        try
        {
            return await memoryCache.GetOrCreateAsync(memKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24);
                await using var context = await contextFactory.CreateDbContextAsync(token);
                var response = await CreateBuildsResponse(request, context, token);
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

    public async Task<List<BuildUpgradeTimingDto>> GetUpgradeTimings(BuildsRequest request, CancellationToken token = default)
    {
        var memKey = $"{request.GetMemKey()}_upgrade_timing";
        try
        {
            return await memoryCache.GetOrCreateAsync(memKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24);
                await using var context = await contextFactory.CreateDbContextAsync(token);
                var timeInfo = Data.GetTimePeriodInfo(request.TimePeriod);
                if (timeInfo is null)
                {
                    return [];
                }

                return await CreateUpgradeTimings(request, timeInfo, context, token);
            }) ?? [];
        }
        catch (OperationCanceledException)
        {
            return [];
        }
    }

    public async Task<List<BuildGasTimingDto>> GetGasTimings(BuildsRequest request, CancellationToken token = default)
    {
        var memKey = $"{request.GetMemKey()}_gas_timing";
        try
        {
            return await memoryCache.GetOrCreateAsync(memKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24);
                await using var context = await contextFactory.CreateDbContextAsync(token);
                var timeInfo = Data.GetTimePeriodInfo(request.TimePeriod);
                if (timeInfo is null)
                {
                    return [];
                }

                return await CreateGasTimings(request, timeInfo, context, token);
            }) ?? [];
        }
        catch (OperationCanceledException)
        {
            return [];
        }
    }

    private async Task<BuildsResponse> CreateBuildsResponse(BuildsRequest request, DsstatsContext context, CancellationToken token)
    {
        var timeInfo = Data.GetTimePeriodInfo(request.TimePeriod);
        if (timeInfo is null)
        {
            return new();
        }

        var noMinRating = request.FromRating <= Data.MinBuildRating;
        var noMaxRating = request.ToRating >= Data.MaxBuildRating;
        var playerIds = request.Players.Select(s => s.PlayerId).ToHashSet();
        var noPlayers = playerIds.Count == 0;

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
                          && (!noPlayers || noMinRating || cpr.RatingBefore > request.FromRating)
                          && (!noPlayers || noMaxRating || cpr.RatingBefore < request.ToRating)
                          && (noPlayers || playerIds.Contains(rp.PlayerId))
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

        var buildStats = await CreateBuildStats(request, timeInfo, context, token);

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

    private async Task<BuildStats> CreateBuildStats(BuildsRequest request, TimePeriodInfo timeInfo, DsstatsContext context, CancellationToken token)
    {
        var playerIds = request.Players.Select(s => s.PlayerId).ToHashSet();
        var noPlayers = playerIds.Count == 0;

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
                    && (!noPlayers || request.FromRating <= Data.MinBuildRating || rpr.RatingBefore >= request.FromRating)
                    && (!noPlayers || request.ToRating >= Data.MaxBuildRating || rpr.RatingBefore <= request.ToRating)
                    && sp.Breakpoint == request.Breakpoint
                    && (noPlayers || playerIds.Contains(rp.PlayerId))
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

    private static async Task<List<BuildUpgradeTimingDto>> CreateUpgradeTimings(
        BuildsRequest request,
        TimePeriodInfo timeInfo,
        DsstatsContext context,
        CancellationToken token)
    {
        var matchingReplayPlayers = GetMatchingBuildReplayPlayers(request, timeInfo, context);
        var cmdrCount = await matchingReplayPlayers.CountAsync(token);
        if (cmdrCount == 0)
        {
            return [];
        }

        var maxUpgradeSecond = GetBreakpointSeconds(request.Breakpoint);
        var query = from rp in matchingReplayPlayers
                    from upgrade in rp.Upgrades
                    where upgrade.Gameloop <= maxUpgradeSecond
                    && !upgrade.Upgrade!.Name.StartsWith("PlayerState")
                    group upgrade by upgrade.Upgrade!.Name into g
                    where g.Count() >= 10
                    orderby g.Average(x => x.Gameloop), g.Count() descending
                    select new BuildUpgradeTimingDto()
                    {
                        Upgrade = g.Key,
                        AverageTimeSeconds = Math.Round(g.Average(x => x.Gameloop), 2),
                        Count = g.Count(),
                        UsagePercent = Math.Round(100.0 * g.Count() / cmdrCount, 2)
                    };

        return await query.ToListAsync(token);
    }

    private static async Task<List<BuildGasTimingDto>> CreateGasTimings(
        BuildsRequest request,
        TimePeriodInfo timeInfo,
        DsstatsContext context,
        CancellationToken token)
    {
        var matchingReplayPlayers = GetMatchingBuildReplayPlayers(request, timeInfo, context);
        var cmdrCount = await matchingReplayPlayers.CountAsync(token);
        if (cmdrCount == 0)
        {
            return [];
        }

        var maxGasSecond = GetBreakpointSeconds(request.Breakpoint);
        var refineryTimings = await matchingReplayPlayers
            .Select(x => x.Refineries)
            .ToListAsync(token);

        Dictionary<int, GasTimingAccumulator> timings = [];
        foreach (var refineries in refineryTimings)
        {
            var gas = 0;
            foreach (var refinerySecond in refineries.Order())
            {
                if (refinerySecond > maxGasSecond)
                {
                    break;
                }

                gas++;
                ref var timing = ref CollectionsMarshal.GetValueRefOrAddDefault(timings, gas, out _);
                timing.Count++;
                timing.TotalSeconds += refinerySecond;
            }
        }

        return timings
            .Where(x => x.Value.Count >= 10)
            .OrderBy(x => x.Key)
            .Select(x => new BuildGasTimingDto()
            {
                Gas = x.Key,
                AverageTimeSeconds = Math.Round(x.Value.TotalSeconds / (double)x.Value.Count, 2),
                Count = x.Value.Count,
                UsagePercent = Math.Round(100.0 * x.Value.Count / cmdrCount, 2)
            })
            .ToList();
    }

    private static IQueryable<ReplayPlayer> GetMatchingBuildReplayPlayers(
        BuildsRequest request,
        TimePeriodInfo timeInfo,
        DsstatsContext context)
    {
        var playerIds = request.Players.Select(s => s.PlayerId).ToHashSet();
        var noPlayers = playerIds.Count == 0;

        return from rp in context.ReplayPlayers.AsNoTracking()
               join sp in context.Spawns.AsNoTracking() on rp.ReplayPlayerId equals sp.ReplayPlayerId
               join r in context.Replays.AsNoTracking() on rp.ReplayId equals r.ReplayId
               join rr in context.ReplayRatings.AsNoTracking() on r.ReplayId equals rr.ReplayId
               join rpr in context.ReplayPlayerRatings.AsNoTracking() on rp.ReplayPlayerId equals rpr.ReplayPlayerId
               where rr.LeaverType == LeaverType.None
               && rr.RatingType == request.RatingType
               && rpr.RatingType == request.RatingType
               && r.Gametime >= timeInfo.Start
               && (!timeInfo.HasEnd || r.Gametime < timeInfo.End)
               && rp.Race == request.Interest
               && (request.Versus == Commander.None || rp.OppRace == request.Versus)
               && (!noPlayers || request.FromRating <= Data.MinBuildRating || rpr.RatingBefore >= request.FromRating)
               && (!noPlayers || request.ToRating >= Data.MaxBuildRating || rpr.RatingBefore <= request.ToRating)
               && sp.Breakpoint == request.Breakpoint
               && (noPlayers || playerIds.Contains(rp.PlayerId))
               select rp;
    }

    private static int GetBreakpointSeconds(Breakpoint breakpoint)
    {
        return breakpoint switch
        {
            Breakpoint.Min5 => 5 * 60,
            Breakpoint.Min10 => 10 * 60,
            Breakpoint.Min15 => 15 * 60,
            _ => int.MaxValue
        };
    }

    private struct GasTimingAccumulator
    {
        public int Count;
        public long TotalSeconds;
    }
}
