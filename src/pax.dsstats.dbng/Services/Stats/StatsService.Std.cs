using Microsoft.EntityFrameworkCore;
using pax.dsstats.shared;

namespace pax.dsstats.dbng.Services;

public partial class StatsService
{
    public async Task<CrossTableResponse> GetCrossTable(CrossTableRequest request, CancellationToken token = default)
    {
        var replays = StdReplaysForCrossTable(request);

        var group = from r in replays
                    group r by new { r.CommandersTeam1, r.CommandersTeam2 } into g
                    select new
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
            teamCrossTables.Add(new()
            {
                Comp = ent.Key,
                TeamResults = ent.Value.Values.ToList(),
            });
        }

        return new CrossTableResponse()
        {
            TeamCrossTables = teamCrossTables,
        };
    }

    private TeamCmdrs? GetTeamCmdrs(string cmdrs, bool std)
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

        replays = replays.Where(x => x.GameMode == GameMode.Standard
            && x.Duration >= 300
            && x.WinnerTeam > 0
            && x.Playercount == 6);

        return replays;
    }
}

