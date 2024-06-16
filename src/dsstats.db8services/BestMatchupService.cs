using dsstats.db8;
using dsstats.shared;
using dsstats.shared.Stats;
using Microsoft.EntityFrameworkCore;

namespace dsstats.db8services;

public class BestMatchupService(ReplayContext context)
{
    public async Task<MatchupResponse> GetBestTeammateResult(MatchupRequest request, CancellationToken token)
    {
        if (request.Commander1 == Commander.None || request.Commander2 == Commander.None)
        {
            return new MatchupResponse() { Request = request };
        }

        (var fromDate, var toDate) = Data.TimeperiodSelected(request.TimePeriod);
        bool withEnd = toDate < DateTime.Today.AddDays(-2);
        var replays = context.Replays
            .Where(x => x.GameMode == GameMode.Commanders
                && x.Duration >= 300
                && x.WinnerTeam > 0
                && x.GameTime >= fromDate
                && (!withEnd || x.GameTime < toDate));

        var teams = await GetTeams(replays, request, token);

        Dictionary<Commander, MatchupCmdrResult> results = [];

        foreach (var team in teams.winners)
        {
            var cmdr = GetThirdCommander(team, request);
            if (cmdr == Commander.None)
            {
                continue;
            }
            if (!results.TryGetValue(cmdr, out var result)
                || result == null)
            {
                result = results[cmdr] = new MatchupCmdrResult() { Commander = cmdr };
            }
            result.Count++;
            result.Wins++;
        }

        foreach (var team in teams.losers)
        {
            var cmdr = GetThirdCommander(team, request);
            if (cmdr == Commander.None)
            {
                continue;
            }
            if (!results.TryGetValue(cmdr, out var result)
                || result == null)
            {
                result = results[cmdr] = new MatchupCmdrResult() { Commander = cmdr };
            }
            result.Count++;
        }

        return new() { Request = request, Results = [.. results.Values] };
    }

    private static Commander GetThirdCommander(string teamString, MatchupRequest request)
    {
        var numbers = teamString.Split('|', StringSplitOptions.RemoveEmptyEntries);

        List<Commander> commanders = [];
        foreach (var number in numbers)
        {
            if (int.TryParse(number, out int n))
            {
                commanders.Add((Commander)n);
            }
        }
        commanders.Remove(request.Commander1);
        commanders.Remove(request.Commander2);
        var thirdCmdr = commanders.FirstOrDefault();
        if ((int)thirdCmdr <= 3)
        {
            thirdCmdr = Commander.None;
        }
        return thirdCmdr;
    }

    private async Task<(List<string> winners, List<string> losers)> GetTeams(IQueryable<Replay> replays,
                                                                             MatchupRequest request,
                                                                             CancellationToken token)
    {
        var cmdrString1 = $"|{(int)request.Commander1}|{(int)request.Commander2}|";
        var cmdrString2 = $"|{(int)request.Commander2}|{(int)request.Commander1}|";

        try
        {
            var winnersTeam1 = await replays.Where(x => x.WinnerTeam == 1
                && (x.CommandersTeam1.Contains(cmdrString1) || x.CommandersTeam1.Contains(cmdrString2)))
            .Select(s => s.CommandersTeam1).ToListAsync(token);
            var winnersTeam2 = await replays.Where(x => x.WinnerTeam == 2
                    && (x.CommandersTeam2.Contains(cmdrString1) || x.CommandersTeam2.Contains(cmdrString2)))
                .Select(s => s.CommandersTeam2).ToListAsync(token);
            var losersTeam1 = await replays.Where(x => x.WinnerTeam == 2
                    && (x.CommandersTeam1.Contains(cmdrString1) || x.CommandersTeam1.Contains(cmdrString2)))
                .Select(s => s.CommandersTeam1).ToListAsync(token);
            var losersTeam2 = await replays.Where(x => x.WinnerTeam == 1
                    && (x.CommandersTeam2.Contains(cmdrString1) || x.CommandersTeam2.Contains(cmdrString2)))
                .Select(s => s.CommandersTeam2).ToListAsync(token);

            return (winnersTeam1.Concat(winnersTeam2).ToList(), losersTeam1.Concat(losersTeam2).ToList());
        }
        catch (OperationCanceledException) { }
        return ([], []);
    }
}

