using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using pax.dsstats.dbng.Extensions;
using pax.dsstats.shared;

namespace pax.dsstats.dbng.Services;

internal record TeamSqlGroup
{
    public string CmdrsT1 { get; set; } = string.Empty;
    public string CmdrsT2 { get; set; } = string.Empty;
    public int Count { get; set; }
    public int WinsT1 { get; set; }
}

internal record TeamGroup
{
    public TeamGroup(TeamSqlGroup teamSqlGroup, bool std = false)
    {
        CmdrsT1 = StatsService.GetTeamCmdrs(teamSqlGroup.CmdrsT1, std);
        CmdrsT2 = StatsService.GetTeamCmdrs(teamSqlGroup.CmdrsT2, std);
        Count = teamSqlGroup.Count;
        WinsT1 = teamSqlGroup.WinsT1;
    }
    public TeamCmdrs? CmdrsT1 { get; set; }
    public TeamCmdrs? CmdrsT2 { get; set; }
    public int Count { get; set; }
    public int WinsT1 { get; set; }
}

public partial class StatsService
{
    public async Task<List<BuildResponseReplay>> GetTeamReplays(CrossTableReplaysRequest request, CancellationToken token)
    {
        var crossTableRequest = new CrossTableRequest()
        {
            Mode = request.Mode,
            TimePeriod = request.TimePeriod,
            TeMaps = request.TeMaps
        };
        var replays = request.Mode == "Standard" ? StdReplaysForCrossTable(crossTableRequest) : CmdrReplaysForCrossTable(crossTableRequest);

        var cmdrString = GetTeamString(request.TeamCmdrs);
        replays = replays.Where(x => x.CommandersTeam1 == cmdrString || x.CommandersTeam2 == cmdrString);

        if (request.TeamCmdrsVs != null)
        {
            var cmdrVsString = GetTeamString(request.TeamCmdrsVs);
            replays = replays.Where(x => x.CommandersTeam1 == cmdrVsString || x.CommandersTeam2 == cmdrVsString);
        }

        return await replays.Select(s => new BuildResponseReplay()
        {
            Hash = s.ReplayHash,
            Gametime = s.GameTime
        }).ToListAsync(token);
    }

    private string GetTeamString(TeamCmdrs teamCmdrs)
    {
        return $"|{(int)teamCmdrs.Cmdrs[0]}|{(int)teamCmdrs.Cmdrs[1]}|{(int)teamCmdrs.Cmdrs[2]}|";
    }

    public async Task<CrossTableResponse> GetCrossTable(CrossTableRequest request, CancellationToken token = default)
    {
        var memkey = request.GenMemKey();

        if (!memoryCache.TryGetValue(memkey, out CrossTableResponse response))
        {
            response = request.Mode switch
            {
                "Standard" => await GetCrossTableStd(request, token),
                "Commanders" => await GetCrossTableCmdr(request, token),
                _ => new()
            };

            memoryCache.Set(memkey, response, new MemoryCacheEntryOptions()
                    .SetPriority(CacheItemPriority.Low)
                    .SetAbsoluteExpiration(TimeSpan.FromDays(3))
                );
        }
        return response;
    }

    private async Task<CrossTableResponse> GetCrossTableCmdr(CrossTableRequest request, CancellationToken token)
    {
        var replays = CmdrReplaysForCrossTable(request);

        var group = from r in replays
                    group r by new { r.CommandersTeam1, r.CommandersTeam2 } into g
                    select new TeamSqlGroup()
                    {
                        CmdrsT1 = g.Key.CommandersTeam1,
                        CmdrsT2 = g.Key.CommandersTeam2,
                        Count = g.Count(),
                        WinsT1 = g.Count(c => c.WinnerTeam == 1),
                    };

        var list = (await group
            .ToListAsync(token)).Select(s => new TeamGroup(s)).ToList();

        list = list.Where(x => x.CmdrsT1 != null && x.CmdrsT2 != null).ToList();

        var lgroup1 = list.GroupBy(g => g.CmdrsT1)
            .Select(s => new
            {
                Team = s.Key,
                Wins = s.Sum(s => s.WinsT1),
                Count = s.Sum(s => s.Count)
            }).ToList();

        var lgroup2 = list.GroupBy(g => g.CmdrsT2)
            .Select(s => new
            {
                Team = s.Key,
                Wins = s.Sum(s => s.Count - s.WinsT1),
                Count = s.Sum(s => s.Count)
            }).ToList();

        Dictionary<TeamCmdrs, TeamCrossTable> results = new();

        foreach (var r in lgroup1)
        {
            if (r.Team == null)
            {
                continue;
            }
            results[r.Team] = new()
            {
                Comp = r.Team,
                Count = r.Count,
                Wins = r.Wins,
                Winrate = Math.Round(r.Wins * 100.0 / r.Count, 2)
            };
        }

        foreach (var r in lgroup2)
        {
            if (r.Team == null)
            {
                continue;
            }
            if (!results.TryGetValue(r.Team, out var s))
            {
                s = results[r.Team] = new() { Comp = r.Team };
            }
            s.Count += r.Count;
            s.Wins += r.Wins;
            s.Winrate = Math.Round(s.Wins * 100.0 / s.Count, 2);
        }

        CrossTableResponse response = new()
        {
            TeamCrossTables = results.Values.Where(x => x.Count >= 10).ToList()
        };

        return response;
    }

    public async Task<CrossTableResponse> GetCrossTableStd(CrossTableRequest request, CancellationToken token = default)
    {
        var replays = StdReplaysForCrossTable(request);

        var group = from r in replays
                    group r by new { r.CommandersTeam1, r.CommandersTeam2 } into g
                    select new TeamSqlGroup()
                    {
                        CmdrsT1 = g.Key.CommandersTeam1,
                        CmdrsT2 = g.Key.CommandersTeam2,
                        Count = g.Count(),
                        WinsT1 = g.Count(c => c.WinnerTeam == 1),
                    };

        var list = await group
            //.Where(x => x.Count > 10)
            .ToListAsync(token);

        var comps = list.Select(s => s.CmdrsT1).ToHashSet();
        comps.UnionWith(list.Select(s => s.CmdrsT2));

        Dictionary<TeamCmdrs, Dictionary<TeamCmdrs, TeamResult>> teamResults = new();

        foreach (var comp in comps)
        {
            var teamCmdrs = GetTeamCmdrs(comp, std: true);
            if (teamCmdrs == null)
            {
                continue;
            }

            var compTeamResult = teamResults[teamCmdrs] = new();

            var resultsT1 = list.Where(x => x.CmdrsT1 == comp).ToList();
            var resultsT2 = list.Where(x => x.CmdrsT2 == comp).ToList();

            foreach (var resultT1 in resultsT1)
            {
                var resultTeam2Cmdrs = GetTeamCmdrs(resultT1.CmdrsT2, std: true);
                if (resultTeam2Cmdrs == null)
                {
                    continue;
                }

                compTeamResult[resultTeam2Cmdrs] = new()
                {
                    Comp = resultTeam2Cmdrs,
                    Count = resultT1.Count,
                    Wins = resultT1.WinsT1
                };
            }

            foreach (var resultT2 in resultsT2)
            {
                var resultTeam1Cmdrs = GetTeamCmdrs(resultT2.CmdrsT1, std: true);
                if (resultTeam1Cmdrs == null)
                {
                    continue;
                }

                if (!compTeamResult.TryGetValue(resultTeam1Cmdrs, out TeamResult? teamResult))
                {
                    teamResult = compTeamResult[resultTeam1Cmdrs] = new()
                    {
                        Comp = resultTeam1Cmdrs,
                    };
                }
                teamResult.Count += resultT2.Count;
                teamResult.Wins += resultT2.Count - resultT2.WinsT1;
            }
        }

        List<TeamCrossTable> teamCrossTables = new();

        foreach (var ent in teamResults)
        {
            int wins = ent.Value.Values.Sum(x => x.Wins);
            int count = ent.Value.Values.Sum(x => x.Count);
            TeamCrossTable result = new()
            {
                Comp = ent.Key,
                TeamResults = ent.Value.Values.ToList(),
                Count = count,
                Wins = wins,
                Winrate = Math.Round(wins * 100.0 / count, 2)
            };
            result.TeamResults.ForEach(f => f.Winrate = Math.Round(f.Wins * 100.0 / f.Count, 2));
            teamCrossTables.Add(result);
        }

        return new CrossTableResponse()
        {
            TeamCrossTables = teamCrossTables,
        };
    }

    public static TeamCmdrs? GetTeamCmdrs(string cmdrs, bool std)
    {
        var ents = cmdrs.Split('|', StringSplitOptions.RemoveEmptyEntries);
        if (ents.Length != 3)
        {
            return null;
        }

        TeamCmdrs teamCmdrs = new();
        for (int i = 0; i < ents.Length; i++)
        {
            int cmdrInt = int.Parse(ents[i]);

            if (std && cmdrInt > 3)
            {
                return null;
            }

            if (!std && cmdrInt <= 3)
            {
                return null;
            }

            teamCmdrs.Cmdrs[i] = (Commander)cmdrInt;
        }
        return teamCmdrs;
    }

    private IQueryable<Replay> StdReplaysForCrossTable(CrossTableRequest request)
    {
        (var start, var end) = Data.TimeperiodSelected(request.TimePeriod);

        var replays = context.Replays

            .Where(x => x.GameTime >= start);

        if (end < DateTime.Today.AddDays(-2))
        {
            replays = replays.Where(x => x.GameTime < end);
        }

        if (request.TeMaps)
        {
            replays = replays.Where(x => x.TournamentEdition);
        }

        replays = replays.Where(x => x.GameMode == GameMode.Standard
            && x.Duration >= 300
            && x.WinnerTeam > 0
            && x.Playercount == 6);

        return replays;
    }

    private IQueryable<Replay> CmdrReplaysForCrossTable(CrossTableRequest request)
    {
        List<GameMode> gameModes = new List<GameMode>() { GameMode.Commanders, GameMode.CommandersHeroic };
        (var start, var end) = Data.TimeperiodSelected(request.TimePeriod);

        var replays = context.Replays

            .Where(x => x.GameTime >= start);

        if (end < DateTime.Today.AddDays(-2))
        {
            replays = replays.Where(x => x.GameTime < end);
        }

        if (request.TeMaps)
        {
            replays = replays.Where(x => x.TournamentEdition);
        }

        replays = replays.Where(x => gameModes.Contains(x.GameMode)
            && x.Duration >= 300
            && x.WinnerTeam > 0
            && x.Playercount == 6);

        return replays;
    }
}

