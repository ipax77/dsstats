using Microsoft.EntityFrameworkCore;
using pax.dsstats.shared;

namespace pax.dsstats.dbng.Services;

public class BuildService
{
    private readonly ReplayContext context;

    public BuildService(ReplayContext context)
    {
        this.context = context;
    }

    public async Task GetBuildResponse(BuildRequest request)
    {
        var replays = GetReuqestReplays(request);

        var groups = (request.Versus == Commander.None, request.PlayerNames.Any()) switch
        {
            (true, true) =>
                            from r in replays
                            from rp in r.ReplayPlayers
                            from s in rp.Spawns
                            from u in s.Units
                            where rp.Race == request.Interest && request.PlayerNames.Contains(rp.Name)
                            group new { s, u } by new { s.Gameloop, u.UnitId } into g
                            select new
                            {
                                Gameloop = g.Key.Gameloop,
                                UnitId = g.Key.UnitId,
                                Sum = g.Sum(c => c.u.Count),
                                Gas = g.Sum(c => c.s.GasCount),
                                Upgrades = g.Sum(c => c.s.UpgradeSpent),
                            },
            (true, false) =>
                            from r in replays
                            from rp in r.ReplayPlayers
                            from s in rp.Spawns
                            from u in s.Units
                            where rp.Race == request.Interest && rp.IsUploader
                            group new { s, u } by new { s.Gameloop, u.UnitId } into g
                            select new
                            {
                                Gameloop = g.Key.Gameloop,
                                UnitId = g.Key.UnitId,
                                Sum = g.Sum(c => c.u.Count),
                                Gas = g.Sum(c => c.s.GasCount),
                                Upgrades = g.Sum(c => c.s.UpgradeSpent),
                            },
            (false, true) =>
                            from r in replays
                            from rp in r.ReplayPlayers
                            from s in rp.Spawns
                            from u in s.Units
                            where rp.Race == request.Interest && request.PlayerNames.Contains(rp.Name) && rp.OppRace == request.Versus
                            group new { s, u } by new { s.Gameloop, u.UnitId } into g
                            select new
                            {
                                Gameloop = g.Key.Gameloop,
                                UnitId = g.Key.UnitId,
                                Sum = g.Sum(c => c.u.Count),
                                Gas = g.Sum(c => c.s.GasCount),
                                Upgrades = g.Sum(c => c.s.UpgradeSpent),
                            },
            (false, false) =>
                            from r in replays
                            from rp in r.ReplayPlayers
                            from s in rp.Spawns
                            from u in s.Units
                            where rp.Race == request.Interest && rp.IsUploader && rp.OppRace == request.Versus
                            group new { s, u } by new { s.Gameloop, u.UnitId } into g
                            select new
                            {
                                Gameloop = g.Key.Gameloop,
                                UnitId = g.Key.UnitId,
                                Sum = g.Sum(c => c.u.Count),
                                Gas = g.Sum(c => c.s.GasCount),
                                Upgrades = g.Sum(c => c.s.UpgradeSpent),
                            }

        };

        var result = await groups
            .AsSplitQuery()
            .ToListAsync();

        Console.WriteLine(result.FirstOrDefault());

    }

    private IQueryable<Replay> GetReuqestReplays(BuildRequest request)
    {
        var replays = context.Replays
            .Include(i => i.ReplayPlayers)
                .ThenInclude(i => i.Spawns)
                .ThenInclude(i => i.Units)
            .Where(x => x.Playercount == 6 && x.Duration > 300 && x.Maxleaver < 90)
            .Where(x => x.ReplayPlayers.Any(a => a.Race == request.Interest))
            .AsNoTracking();

        replays = replays.Where(x => x.GameTime >= request.StartTime);
        if (request.EndTime != DateTime.Today)
        {
            replays = replays.Where(x => x.GameTime <= request.EndTime);
        }

        if ((int)request.Interest > 3)
        {
            replays = replays.Where(x => x.GameMode == GameMode.Commanders || x.GameMode == GameMode.CommandersHeroic);
        }
        else
        {
            replays = replays.Where(x => x.GameMode == GameMode.Standard);
        }
        return replays;
    }


    public async Task<BuildResponse> GetBuild(BuildRequest request)
    {
        var replays = context.Replays
            .Include(i => i.ReplayPlayers)
                .ThenInclude(i => i.Spawns)
                .ThenInclude(i => i.Units)
            .Where(x => x.Playercount == 6 && x.Duration > 300 && x.Maxleaver < 90)
            .AsNoTracking();

        replays = replays.Where(x => x.GameTime >= request.StartTime);
        if (request.EndTime != DateTime.Today)
        {
            replays = replays.Where(x => x.GameTime <= request.EndTime);
        }

        if ((int)request.Interest > 3)
        {
            replays = replays.Where(x => x.GameMode == GameMode.Commanders || x.GameMode == GameMode.CommandersHeroic);
        }
        else
        {
            replays = replays.Where(x => x.GameMode == GameMode.Standard);
        }


        var buildResults = GetBuildResultQuery(replays, request);

        var builds = await buildResults.AsSplitQuery().ToListAsync();
        var uniqueBuilds = builds.GroupBy(g => g.Id).Select(s => s.First()).ToList();


        var response = new BuildResponse()
        {
            Interest = request.Interest,
            Versus = request.Versus,
            Count = uniqueBuilds.Count,
            Wins = uniqueBuilds.Where(s => s.Result == PlayerResult.Win).Count(),
            Duration = uniqueBuilds.Sum(s => s.Duration),
            Gas = uniqueBuilds.Sum(s => s.GasCount),
            Upgrades = uniqueBuilds.Sum(s => s.UpgradeSpending),
            Replays = uniqueBuilds.Select(t => new BuildResponseReplay()
            {
                Hash = t.Hash,
                Gametime = t.Gametime
            }).ToList(),
            Breakpoints = new List<BuildResponseBreakpoint>()
        };


        foreach (Breakpoint bp in Enum.GetValues(typeof(Breakpoint)))
        {
            var bpReplays = builds.Where(x => Data.GetBreakpoint(x.Gameloop) == bp).ToList();

            response.Breakpoints.Add(new BuildResponseBreakpoint()
            {
                Breakpoint = bp.ToString(),
                Count = bpReplays.Count,
                Wins = bpReplays.Where(x => x.Result == PlayerResult.Win).Count(),
                Duration = bpReplays.Sum(s => s.Duration),
                Gas = bpReplays.Sum(s => s.GasCount),
                Upgrades = bpReplays.Sum(s => s.UpgradeSpending),
                Units = await GetUnits(bpReplays.Select(s => s.Units).ToList())
            });
        }
        return response;
    }

    public static IQueryable<BuildHelper> GetBuildResultQuery(IQueryable<Replay> replays, BuildRequest request)
    {
        return (request.Versus == Commander.None, !request.PlayerNames.Any()) switch
        {
            (true, true) => from r in replays
                            from p in r.ReplayPlayers
                            from s in p.Spawns
                            from u in s.Units
                            where p.Race == request.Interest && p.IsUploader
                            select new BuildHelper()
                            {
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
                             select new BuildHelper()
                             {
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
                             select new BuildHelper()
                             {
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
                              select new BuildHelper()
                              {
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

    private async Task<List<BuildResponseBreakpointUnit>> GetUnits(List<List<KeyValuePair<int, int>>> spawnsUnits)
    {
        Dictionary<int, int> unitSums = new();

        foreach (var spawn in spawnsUnits)
        {
            foreach (var unit in spawn)
            {
                if (!unitSums.ContainsKey(unit.Key))
                {
                    unitSums[unit.Key] = unit.Value;
                }
                else
                {
                    unitSums[unit.Key] += unit.Value;
                }
            }
        }
        List<BuildResponseBreakpointUnit> units = new();

        foreach (var ent in unitSums)
        {
            units.Add(new()
            {
                Name = await GetUnitName(ent.Key),
                Count = ent.Value
            });
        }

        return units;
    }

    private async Task<string> GetUnitName(int unitId)
    {
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

