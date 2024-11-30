using dsstats.db8;
using dsstats.shared;
using dsstats.shared.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Frozen;

namespace dsstats.db8services;

public class TeamcompService(ReplayContext context, IMemoryCache memoryCache) : ITeamcompService
{
    private static readonly FrozenDictionary<string, bool> stdComps = new Dictionary<string, bool>()
    {
        { "|1|1|1|", true },
        { "|1|1|2|", true },
        { "|1|1|3|", true },
        { "|1|2|1|", true },
        { "|1|2|2|", true },
        { "|1|2|3|", true },
        { "|1|3|1|", true },
        { "|1|3|2|", true },
        { "|1|3|3|", true },
        { "|2|1|1|", true },
        { "|2|1|2|", true },
        { "|2|1|3|", true },
        { "|2|2|1|", true },
        { "|2|2|2|", true },
        { "|2|2|3|", true },
        { "|2|3|1|", true },
        { "|2|3|2|", true },
        { "|2|3|3|", true },
        { "|3|1|1|", true },
        { "|3|1|2|", true },
        { "|3|1|3|", true },
        { "|3|2|1|", true },
        { "|3|2|2|", true },
        { "|3|2|3|", true },
        { "|3|3|1|", true },
        { "|3|3|2|", true },
        { "|3|3|3|", true },
    }.ToFrozenDictionary();

    public async Task<int> GetReplaysCount(TeamcompReplaysRequest request, CancellationToken token = default)
    {
        var query = GetReplays(request);
        return await query
            .CountAsync(token);
    }

    public async Task<List<ReplayListDto>> GetReplays(TeamcompReplaysRequest request, CancellationToken token)
    {
        var query = GetReplays(request);

        return await query
            .OrderByDescending(o => o.GameTime)
            .Skip(request.Skip)
            .Take(request.Take)
            .Select(s => new ReplayListDto()
            {
                GameTime = s.GameTime,
                Duration = s.Duration,
                WinnerTeam = s.WinnerTeam,
                GameMode = s.GameMode,
                TournamentEdition = s.TournamentEdition,
                ReplayHash = s.ReplayHash,
                DefaultFilter = s.DefaultFilter,
                CommandersTeam1 = s.CommandersTeam1,
                CommandersTeam2 = s.CommandersTeam2,
                MaxLeaver = s.Maxleaver
            })
            .ToListAsync(token);
    }

    private IQueryable<Replay> GetReplays(TeamcompReplaysRequest request)
    {
        (var startDate, var endDate) = Data.TimeperiodSelected(request.TimePeriod);
        var tillDate = DateTime.Today.AddDays(-2);
        bool withoutOpp = string.IsNullOrEmpty(request.Team2);

        return from r in context.Replays
               where r.GameTime >= startDate
                && (endDate > tillDate || r.GameTime < endDate)
                && r.GameMode == GameMode.Standard
                && r.Playercount == 6 && r.Duration >= 300 && r.WinnerTeam > 0
                && (!request.TournementEdition || r.TournamentEdition)
                && (withoutOpp ? (r.CommandersTeam1 == request.Team1 || r.CommandersTeam2 == request.Team2)
                : ((r.CommandersTeam1 == request.Team1 && r.CommandersTeam2 == request.Team2)
                  || (r.CommandersTeam2 == request.Team1 && r.CommandersTeam1 == request.Team2)))
               select r;
    }

    public async Task<TeamcompResponse> GetTeamcompResult(TeamcompRequest request, CancellationToken token = default)
    {
        var memKey = request.GenMemKey();
        if (!memoryCache.TryGetValue(memKey, out TeamcompResponse? result)
            || result is null)
        {
            result = request.TournamentEdition ? 
                await ProduceTeTeamcompResult(request, token)
                : await ProduceTeamcompResult(request, token);
            memoryCache.Set(memKey, result, TimeSpan.FromHours(24));
        }
        return result;
    }

    private async Task<TeamcompResponse> ProduceTeamcompResult(TeamcompRequest request, CancellationToken token)
    {
        (var startDate, var endDate) = Data.TimeperiodSelected(request.TimePeriod);
        var tillDate = DateTime.Today.AddDays(-2);
        var noInterest = string.IsNullOrEmpty(request.Interest);

        var group1 = from r in context.Replays
                     from rp in r.ReplayPlayers
                     join rr in context.ComboReplayRatings on r.ReplayId equals rr.ReplayId
                     join rpr in context.ComboReplayPlayerRatings on rp.ReplayPlayerId equals rpr.ReplayPlayerId
                     where r.GameTime >= startDate
                      && (endDate > tillDate || r.GameTime < endDate)
                      && rr.RatingType == request.RatingType
                      && (request.WithLeavers || rr.LeaverType == LeaverType.None)
                      && rp.Team == 1
                      && (noInterest || r.CommandersTeam2 == request.Interest)
                     group new { r, rpr } by r.CommandersTeam1 into g
                     select new TeamGroupResult()
                     {
                         Cmdrs = g.Key,
                         Count = g.Count(),
                         Wins = g.Count(c => c.r.WinnerTeam == 1),
                         AvgGain = Math.Round(g.Average(a => a.rpr.Change), 2)
                     };

        var group2 = from r in context.Replays
                     from rp in r.ReplayPlayers
                     join rr in context.ComboReplayRatings on r.ReplayId equals rr.ReplayId
                     join rpr in context.ComboReplayPlayerRatings on rp.ReplayPlayerId equals rpr.ReplayPlayerId
                     where r.GameTime >= startDate
                      && (endDate > tillDate || r.GameTime < endDate)
                      && rr.RatingType == request.RatingType
                      && (request.WithLeavers || rr.LeaverType == LeaverType.None)
                      && rp.Team == 2
                      && (noInterest || r.CommandersTeam1 == request.Interest)
                     group new { r, rpr } by r.CommandersTeam2 into g
                     select new TeamGroupResult()
                     {
                         Cmdrs = g.Key,
                         Count = g.Count(),
                         Wins = g.Count(c => c.r.WinnerTeam == 2),
                         AvgGain = Math.Round(g.Average(a => a.rpr.Change), 2)
                     };

        var l1 = await group1.ToListAsync(token);
        var l2 = await group2.ToListAsync(token);

        var items = CombineResults(l1, l2);
        return new()
        {
            Team = request.Interest,
            Items = items
        };
    }

    private async Task<TeamcompResponse> ProduceTeTeamcompResult(TeamcompRequest request, CancellationToken token)
    {
        (var startDate, var endDate) = Data.TimeperiodSelected(request.TimePeriod);
        var tillDate = DateTime.Today.AddDays(-2);
        var noInterest = string.IsNullOrEmpty(request.Interest);

        var group1 = from r in context.Replays
                     from rp in r.ReplayPlayers
                     join rr in context.ReplayRatings on r.ReplayId equals rr.ReplayId
                     join rpr in context.RepPlayerRatings on rp.ReplayPlayerId equals rpr.ReplayPlayerId
                     where r.GameTime >= startDate
                      && (endDate > tillDate || r.GameTime < endDate)
                      && rr.RatingType == RatingType.StdTE
                      && (request.WithLeavers || rr.LeaverType == LeaverType.None)
                      && rp.Team == 1
                      && (noInterest || r.CommandersTeam2 == request.Interest)
                     group new { r, rpr } by r.CommandersTeam1 into g
                     select new TeamGroupResult()
                     {
                         Cmdrs = g.Key,
                         Count = g.Count(),
                         Wins = g.Count(c => c.r.WinnerTeam == 1),
                         AvgGain = Math.Round(g.Average(a => a.rpr.RatingChange), 2)
                     };

        var group2 = from r in context.Replays
                     from rp in r.ReplayPlayers
                     join rr in context.ReplayRatings on r.ReplayId equals rr.ReplayId
                     join rpr in context.RepPlayerRatings on rp.ReplayPlayerId equals rpr.ReplayPlayerId
                     where r.GameTime >= startDate
                      && (endDate > tillDate || r.GameTime < endDate)
                      && rr.RatingType == RatingType.StdTE
                      && (request.WithLeavers || rr.LeaverType == LeaverType.None)
                      && rp.Team == 2
                      && (noInterest || r.CommandersTeam1 == request.Interest)
                     group new { r, rpr } by r.CommandersTeam2 into g
                     select new TeamGroupResult()
                     {
                         Cmdrs = g.Key,
                         Count = g.Count(),
                         Wins = g.Count(c => c.r.WinnerTeam == 2),
                         AvgGain = Math.Round(g.Average(a => a.rpr.RatingChange), 2)
                     };

        var l1 = await group1.ToListAsync(token);
        var l2 = await group2.ToListAsync(token);

        var items = CombineResults(l1, l2);
        return new()
        {
            Team = request.Interest,
            Items = items
        };
    }

    private List<TeamResponseItem> CombineResults(List<TeamGroupResult> results1, List<TeamGroupResult> results2)
    {
        List<TeamResponseItem> items = new();

        foreach (var r1 in results1)
        {
            if (!stdComps.ContainsKey(r1.Cmdrs))
            {
                continue;
            }

            var r2 = results2.FirstOrDefault(f => f.Cmdrs == r1.Cmdrs);
            if (r2 != null)
            {
                results2.Remove(r2);
            }

            int count = r1.Count + (r2?.Count ?? 0);

            items.Add(new()
            {
                Team = r1.Cmdrs,
                Count = count,
                Wins = r1.Wins + (r2?.Wins ?? 0),
                AvgGain = r2 == null ? r1.AvgGain
                : Math.Round(((r1.Count * r1.AvgGain) + (r2.Count * r2.AvgGain)) / count, 2)
            });
        }

        foreach (var r2 in results2)
        {
            if (!stdComps.ContainsKey(r2.Cmdrs))
            {
                continue;
            }

            items.Add(new()
            {
                Team = r2.Cmdrs,
                Count = r2.Count,
                Wins = r2.Wins,
                AvgGain = r2.AvgGain
            });
        }
        return items;
    }
}

internal record TeamGroupResult
{
    public string Cmdrs { get; init; } = string.Empty;
    public int Count { get; init; }
    public int Wins { get; init; }
    public double AvgGain { get; init; }
}