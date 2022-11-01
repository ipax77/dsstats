using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using pax.dsstats.dbng.Extensions;
using pax.dsstats.shared;
using System.Diagnostics;

namespace pax.dsstats.dbng.Services;

public class BuildService
{
    private readonly ReplayContext context;
    private readonly IMemoryCache memoryCache;
    private readonly ILogger<BuildService> logger;

    public BuildService(ReplayContext context, IMemoryCache memoryCache, ILogger<BuildService> logger)
    {
        this.context = context;
        this.memoryCache = memoryCache;
        this.logger = logger;
    }

    public async Task<BuildResponse> GetBuild(BuildRequest buildRequest, Dictionary<int, string>? units = null)
    {
        var memKey = buildRequest.GenMemKey();

        if (!memoryCache.TryGetValue(memKey, out BuildResponse buildResponse)) {
            buildResponse = await GetBuildFromDb(buildRequest, units);

            memoryCache.Set(memKey, buildResponse, new MemoryCacheEntryOptions()
            .SetPriority(CacheItemPriority.High)
            .SetAbsoluteExpiration(TimeSpan.FromDays(7)));

        }
        return buildResponse;
    }

    public async Task SeedBuildsCache()
    {
        BuildRequest request = new() {
            PlayerNames = new() { "PAX" },
        };

        Dictionary<int, string> units = (await context.Units
            .AsNoTracking()
            .Select(s => new { s.UnitId, s.Name })
            .ToListAsync())
            .ToDictionary(x => x.UnitId, y => y.Name);

        Stopwatch sw = new();
        sw.Start();

        foreach (Commander cmdr in Data.GetCommanders(Data.CmdrGet.NoNone)) {
            foreach (Commander cmdrVs in Data.GetCommanders(Data.CmdrGet.All)) {
                request.Interest = cmdr;
                request.Versus = cmdrVs;
                await GetBuild(request, units);
            }
        }
        sw.Stop();
        logger.LogWarning($"buildCache built in {sw.ElapsedMilliseconds} ms");
    }

    private async Task<BuildResponse> GetBuildFromDb(BuildRequest request, Dictionary<int, string>? units = null)
    {
        var replays = context.Replays
            .Include(i => i.ReplayPlayers)
                .ThenInclude(i => i.Spawns)
                .ThenInclude(i => i.Units)
            .Where(x => x.DefaultFilter)
            .AsNoTracking();

        replays = replays.Where(x => x.GameTime >= request.StartTime);
        if (request.EndTime != DateTime.Today) {
            replays = replays.Where(x => x.GameTime <= request.EndTime);
        }

        if ((int)request.Interest > 3) {
            replays = replays.Where(x => x.GameMode == GameMode.Commanders || x.GameMode == GameMode.CommandersHeroic);
        } else {
            replays = replays.Where(x => x.GameMode == GameMode.Standard);
        }


        var buildResults = GetBuildResultQuery(replays, request);

        var builds = await buildResults.AsSplitQuery().ToListAsync();
        var uniqueBuilds = builds.GroupBy(g => g.Id).Select(s => s.First()).ToList();


        var response = new BuildResponse() {
            Interest = request.Interest,
            Versus = request.Versus,
            Count = uniqueBuilds.Count,
            Wins = uniqueBuilds.Where(s => s.Result == PlayerResult.Win).Count(),
            Duration = uniqueBuilds.Sum(s => s.Duration),
            Gas = uniqueBuilds.Sum(s => s.GasCount),
            Upgrades = uniqueBuilds.Sum(s => s.UpgradeSpending),
            Replays = uniqueBuilds.Select(t => new BuildResponseReplay() {
                Hash = t.Hash,
                Gametime = t.Gametime
            }).ToList(),
            Breakpoints = new List<BuildResponseBreakpoint>()
        };


        foreach (Breakpoint bp in Enum.GetValues(typeof(Breakpoint))) {
            var bpReplays = builds.Where(x => Data.GetBreakpoint(x.Gameloop) == bp).ToList();

            response.Breakpoints.Add(new BuildResponseBreakpoint() {
                Breakpoint = bp.ToString(),
                Count = bpReplays.Count,
                Wins = bpReplays.Where(x => x.Result == PlayerResult.Win).Count(),
                Duration = bpReplays.Sum(s => s.Duration),
                Gas = bpReplays.Sum(s => s.GasCount),
                Upgrades = bpReplays.Sum(s => s.UpgradeSpending),
                Units = await GetUnits(bpReplays.Select(s => s.Units).ToList(), units)
            });
        }
        return response;
    }

    private static IQueryable<BuildHelper> GetBuildResultQuery(IQueryable<Replay> replays, BuildRequest request)
    {
        return (request.Versus == Commander.None, !request.PlayerNames.Any()) switch {
            (true, true) => from r in replays
                            from p in r.ReplayPlayers
                            from s in p.Spawns
                            from u in s.Units
                            where p.Race == request.Interest && p.IsUploader
                            select new BuildHelper() {
                                Id = r.ReplayId,
                                Hash = r.ReplayHash,
                                Gametime = r.GameTime,
                                Units = s.Units.Select(s => new KeyValuePair<int, int>(s.UnitId, s.Count)).ToList(),
                                Result = p.PlayerResult,
                                UpgradeSpending = s.UpgradeSpent,
                                GasCount = s.GasCount,
                                Gameloop = s.Gameloop,
                                Duration = r.Duration
                            },
            (true, false) => from r in replays
                             from p in r.ReplayPlayers
                             from s in p.Spawns
                             from u in s.Units
                             where p.Race == request.Interest && request.PlayerNames.Contains(p.Name)
                             select new BuildHelper() {
                                 Id = r.ReplayId,
                                 Hash = r.ReplayHash,
                                 Gametime = r.GameTime,
                                 Units = s.Units.Select(s => new KeyValuePair<int, int>(s.UnitId, s.Count)).ToList(),
                                 Result = p.PlayerResult,
                                 UpgradeSpending = s.UpgradeSpent,
                                 GasCount = s.GasCount,
                                 Gameloop = s.Gameloop,
                                 Duration = r.Duration
                             },
            (false, true) => from r in replays
                             from p in r.ReplayPlayers
                             from s in p.Spawns
                             from u in s.Units
                             where p.Race == request.Interest && p.OppRace == request.Versus && p.IsUploader
                             select new BuildHelper() {
                                 Id = r.ReplayId,
                                 Hash = r.ReplayHash,
                                 Gametime = r.GameTime,
                                 Units = s.Units.Select(s => new KeyValuePair<int, int>(s.UnitId, s.Count)).ToList(),
                                 Result = p.PlayerResult,
                                 UpgradeSpending = s.UpgradeSpent,
                                 GasCount = s.GasCount,
                                 Gameloop = s.Gameloop,
                                 Duration = r.Duration
                             },
            (false, false) => from r in replays
                              from p in r.ReplayPlayers
                              from s in p.Spawns
                              from u in s.Units
                              where p.Race == request.Interest && p.OppRace == request.Versus && request.PlayerNames.Contains(p.Name)
                              select new BuildHelper() {
                                  Id = r.ReplayId,
                                  Hash = r.ReplayHash,
                                  Gametime = r.GameTime,
                                  Units = s.Units.Select(s => new KeyValuePair<int, int>(s.UnitId, s.Count)).ToList(),
                                  Result = p.PlayerResult,
                                  UpgradeSpending = s.UpgradeSpent,
                                  GasCount = s.GasCount,
                                  Gameloop = s.Gameloop,
                                  Duration = r.Duration
                              },
        };
    }

    private async Task<List<BuildResponseBreakpointUnit>> GetUnits(List<List<KeyValuePair<int, int>>> spawnsUnits, Dictionary<int, string>? units = null)
    {
        Dictionary<int, int> unitSums = new();

        foreach (var spawn in spawnsUnits) {
            foreach (var unit in spawn) {
                if (!unitSums.ContainsKey(unit.Key)) {
                    unitSums[unit.Key] = unit.Value;
                } else {
                    unitSums[unit.Key] += unit.Value;
                }
            }
        }
        List<BuildResponseBreakpointUnit> bpUnits = new();

        foreach (var ent in unitSums) {
            bpUnits.Add(new() {
                Name = await GetUnitName(ent.Key, units),
                Count = ent.Value
            });
        }

        return bpUnits;
    }

    private async Task<string> GetUnitName(int unitId, Dictionary<int, string>? units = null)
    {
        if (units != null) {
            if (units.ContainsKey(unitId)) {
                return units[unitId];
            } else {
                return "";
            }
        }

        var name = await context.Units
            .Where(x => x.UnitId == unitId)
            .AsNoTracking()
            .Select(x => x.Name)
            .FirstOrDefaultAsync();
        return name ?? "";
    }

}


public record BuildHelper
{
    public int Id { get; init; }
    public string Hash { get; init; } = null!;
    public DateTime Gametime { get; init; }
    public List<KeyValuePair<int, int>> Units { get; init; } = new();
    public PlayerResult Result { get; init; }
    public int UpgradeSpending { get; init; }
    public int GasCount { get; init; }
    public int Gameloop { get; init; }
    public int Duration { get; init; }
}

