using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using pax.dsstats.dbng.Extensions;
using pax.dsstats.shared;

namespace pax.dsstats.dbng.Services;

public partial class TeamService : ITeamService
{
    private readonly ReplayContext context;
    private readonly IMemoryCache memoryCache;

    public TeamService(ReplayContext context, IMemoryCache memoryCache)
    {
        this.context = context;
        this.memoryCache = memoryCache;
    }

    public async Task<TeamCompResponse> GetTeamRating(TeamCompRequest request, CancellationToken token)
    {
        var memKey = request.GenMemKey();
        if (!memoryCache.TryGetValue(memKey, out TeamCompResponse result))
        {
            result = await ProduceTeamRating(request, token);
            memoryCache.Set(memKey, result, TimeSpan.FromHours(24));
        }
        return result;
    }

    private async Task<TeamCompResponse> ProduceTeamRating(TeamCompRequest request, CancellationToken token)
    {
        var replays = GetReplays(request);
        var noInterest = string.IsNullOrEmpty(request.Interest);

        var group1 = from r in replays
                     from rp in r.ReplayPlayers
                     where rp.Team == 1 && (int)rp.Race <= 3 && (int)rp.OppRace <= 3
                        && (noInterest ? true : r.CommandersTeam2 == request.Interest)
                     group new { r, rp } by r.CommandersTeam1 into g
                     select new TeamGroupResult()
                     {
                         Cmdrs = g.Key,
                         Count = g.Count(),
                         Wins = g.Count(c => c.r.WinnerTeam == 1),
                         AvgGain = Math.Round(g.Sum(a => a.rp.ReplayPlayerRatingInfo!.RatingChange), 2)
                     };

        var group2 = from r in replays
                     from rp in r.ReplayPlayers
                     where rp.Team == 2 && (int)rp.Race <= 3 && (int)rp.OppRace <= 3
                        && (noInterest ? true : r.CommandersTeam1 == request.Interest)
                     group new { r, rp } by r.CommandersTeam2 into g
                     select new TeamGroupResult()
                     {
                         Cmdrs = g.Key,
                         Count = g.Count(),
                         Wins = g.Count(c => c.r.WinnerTeam == 2),
                         AvgGain = Math.Round(g.Sum(a => a.rp.ReplayPlayerRatingInfo!.RatingChange), 2)
                     };

        var l1 = await group1.ToListAsync(token);
        var l2 = await group2.ToListAsync(token);

        var combined = CombineResults(l1, l2);

        return new()
        {
            Team = request.Interest,
            Items = combined,
            Replays = await GetReplayInfos(request, replays, token)
        };
    }

    private async Task<List<TeamReplayInfo>> GetReplayInfos(TeamCompRequest request, IQueryable<Replay> replays, CancellationToken token)
    {
        if (string.IsNullOrEmpty(request.Interest))
        {
            return new();
        }
        else
        {
            return await replays
                .OrderByDescending(o => o.GameTime)
                .Where(x => x.CommandersTeam1 == request.Interest || x.CommandersTeam2 == request.Interest)
                .Select(s => new TeamReplayInfo()
                {
                    GameTime = s.GameTime,
                    ReplayHash = s.ReplayHash,
                    CommandersTeam1 = s.CommandersTeam1,
                    CommandersTeam2 = s.CommandersTeam2,
                }).ToListAsync();
        }
    }

    private List<TeamResponseItem> CombineResults(List<TeamGroupResult> l1s, List<TeamGroupResult> l2s)
    {
        List<string> cmdrs1 = l1s.Select(s => s.Cmdrs).ToList();
        List<string> cmdrs2 = l2s.Select(s => s.Cmdrs).ToList();
        List<TeamResponseItem> items = new();

        foreach (var l1 in l1s)
        {
            var l2 = l2s.FirstOrDefault(f => f.Cmdrs == l1.Cmdrs);

            if (l2 != null)
            {
                cmdrs2.Remove(l2.Cmdrs);
            }

            items.Add(new()
            {
                Team = l1.Cmdrs,
                Count = l2 == null ? l1.Count : l1.Count + l2.Count,
                Wins = l2 == null ? l1.Wins : l1.Wins + l2.Wins,
                AvgGain = l2 == null ? l1.AvgGain : l1.AvgGain + l2.AvgGain,
            });
        }

        foreach (var cmdrs in cmdrs2)
        {
            var l2 = l2s.FirstOrDefault(f => f.Cmdrs == cmdrs);

            if (l2 == null)
            {
                continue;
            }

            items.Add(new()
            {
                Team = cmdrs,
                Count = l2.Count,
                Wins = l2.Wins,
                AvgGain = l2.AvgGain,
            });
        }

        items.ForEach(f =>
            {
                f.AvgGain = Math.Round((f.AvgGain / f.Count), 2);
                f.Count = f.Count / 3;
                f.Wins = f.Wins / 3;
            });
        return items;
    }

    private IQueryable<Replay> GetReplays(TeamCompRequest request)
    {
        (var startDate, var endDate) = Data.TimeperiodSelected(request.TimePeriod);

        var replays = context.Replays
            .Where(x => x.GameTime >= startDate);

        if (endDate < DateTime.Today.AddDays(-2))
        {
            replays = replays.Where(x => x.GameTime < endDate);
        }

        replays = replays
            .Where(x => x.ReplayRatingInfo != null
                && x.ReplayRatingInfo.RatingType == request.RatingType);

        if (!request.WithLeavers)
        {
            replays = replays.Where(x => x.ReplayRatingInfo!.LeaverType == LeaverType.None);
        }

        return replays;
    }
}

internal record TeamGroupResult
{
    public string Cmdrs { get; init; } = string.Empty;
    public int Count { get; init; }
    public int Wins { get; init; }
    public double AvgGain { get; init; }
}



