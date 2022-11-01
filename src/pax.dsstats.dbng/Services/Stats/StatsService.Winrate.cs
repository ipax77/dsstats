﻿using Microsoft.EntityFrameworkCore;
using pax.dsstats.shared;

namespace pax.dsstats.dbng.Services;

public partial class StatsService
{
    private readonly List<GameMode> defaultGameModes = new List<GameMode>() { GameMode.Commanders, GameMode.CommandersHeroic };

    private async Task<StatsResponse> GetWinrate(StatsRequest request)
    {
        if (!request.DefaultFilter) {
            return await GetCustomWinrate(request);
        }

        var firstNotSecond = request.GameModes.Except(defaultGameModes).ToList();
        var secondNotFirst = defaultGameModes.Except(request.GameModes).ToList();

        if (firstNotSecond.Any() || secondNotFirst.Any()) {
            return await GetCustomWinrate(request);
        }

        var cmdrstats = await GetRequestStats(request);

        DateTime endTime = request.EndTime == DateTime.MinValue ? DateTime.Today.AddDays(1) : request.EndTime;

        var stats = cmdrstats.Where(x => x.Time >= request.StartTime && x.Time <= endTime);

        if (request.Interest != Commander.None) {
            stats = stats.Where(x => x.Race == request.Interest).ToList();
        }

        if (!stats.Any()) {
            return new StatsResponse() {
                Request = request,
                Items = new List<StatsResponseItem>(),
            };
        }

        var data = request.Interest == Commander.None ?
            stats.GroupBy(g => g.Race).Select(s => new StatsResponseItem() {
                Label = s.Key.ToString(),
                Matchups = s.Sum(c => c.Count),
                Wins = s.Sum(c => c.Wins),
                duration = (long)s.Sum(c => c.Duration)
            }).ToList()
            :
            stats.GroupBy(g => g.OppRace).Select(s => new StatsResponseItem() {
                Label = s.Key.ToString(),
                Matchups = s.Sum(c => c.Count),
                Wins = s.Sum(c => c.Wins),
                duration = (long)s.Sum(c => c.Duration)
            }).ToList();


        return new StatsResponse() {
            Request = request,
            Items = data,
            CountResponse = await GetCount(request),
            AvgDuration = !data.Any() ? 0 : Convert.ToInt32(data.Select(s => s.duration / (double)s.Matchups).Average())
        };
    }

    public async Task<StatsResponse> GetCustomWinrate(StatsRequest request)
    {
        var replays = GetCountReplays(request);

        var responses = (request.Uploaders, request.Interest == Commander.None) switch {
            (false, true) => from r in replays
                             from p in r.ReplayPlayers
                             group new { r, p } by new { race = p.Race } into g
                             select new StatsResponseItem() {
                                 Label = g.Key.race.ToString(),
                                 Matchups = g.Count(),
                                 Wins = g.Count(c => c.p.PlayerResult == PlayerResult.Win),
                                 duration = g.Sum(s => s.r.Duration)
                             },
            (false, false) => from r in replays
                              from p in r.ReplayPlayers
                              where p.Race == request.Interest
                              group new { r, p } by new { race = p.OppRace } into g
                              select new StatsResponseItem() {
                                  Label = g.Key.race.ToString(),
                                  Matchups = g.Count(),
                                  Wins = g.Count(c => c.p.PlayerResult == PlayerResult.Win),
                                  duration = g.Sum(s => s.r.Duration)
                              },
            (true, true) => from r in replays
                            from p in r.ReplayPlayers
                            where p.IsUploader
                            group new { r, p } by new { race = p.Race } into g
                            select new StatsResponseItem() {
                                Label = g.Key.race.ToString(),
                                Matchups = g.Count(),
                                Wins = g.Count(c => c.p.PlayerResult == PlayerResult.Win),
                                duration = g.Sum(s => s.r.Duration)
                            },
            (true, false) => from r in replays
                             from p in r.ReplayPlayers
                             where p.IsUploader && p.Race == request.Interest
                             group new { r, p } by new { race = p.OppRace } into g
                             select new StatsResponseItem() {
                                 Label = g.Key.race.ToString(),
                                 Matchups = g.Count(),
                                 Wins = g.Count(c => c.p.PlayerResult == PlayerResult.Win),
                                 duration = g.Sum(s => s.r.Duration)
                             },
        };

        var items = await responses.ToListAsync();

        if (!items.Any()) {
            return new StatsResponse() {
                Request = request,
                Items = new List<StatsResponseItem>()
            };
        }

        return new StatsResponse() {
            Request = request,
            Items = items,
            CountResponse = await GetCount(request),
            AvgDuration = !items.Any() ? 0 : Convert.ToInt32(items.Select(s => s.duration / (double)s.Matchups).Average())
        };
    }
}
