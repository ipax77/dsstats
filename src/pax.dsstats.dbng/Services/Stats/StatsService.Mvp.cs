using Microsoft.EntityFrameworkCore;
using pax.dsstats.shared;

namespace pax.dsstats.dbng.Services;

public partial class StatsService
{
    private async Task<StatsResponse> GetMvp(StatsRequest request)
    {
        if (!request.DefaultFilter || request.TeMaps)
        {
            return await GetCustomMvp(request);
        }

        var firstNotSecond = request.GameModes.Except(defaultGameModes).ToList();
        var secondNotFirst = defaultGameModes.Except(request.GameModes).ToList();

        if (firstNotSecond.Any() || secondNotFirst.Any())
        {
            return await GetCustomMvp(request);
        }

        if (request.PlayerNames.Any())
        {
            return await GetCustomMvp(request);
        }

        var cmdrstats = await GetRequestStats(request);

        (var startTime, var endTime) = Data.TimeperiodSelected(request.TimePeriod);

        var stats = cmdrstats.Where(x => x.Time >= startTime && x.Time <= endTime);

        if (request.Interest != Commander.None)
        {
            stats = stats.Where(x => x.Race == request.Interest).ToList();
        }

        if (!stats.Any())
        {
            return new StatsResponse()
            {
                Request = request,
                Items = new List<StatsResponseItem>(),
            };
        }

        var data = request.Interest == Commander.None ?
            stats.GroupBy(g => g.Race).Select(s => new StatsResponseItem()
            {
                Label = s.Key.ToString(),
                Matchups = s.Sum(c => c.Count),
                Wins = s.Sum(c => c.Mvp),
                duration = (long)s.Sum(c => c.Duration)
            }).ToList()
            :
            stats.GroupBy(g => g.OppRace).Select(s => new StatsResponseItem()
            {
                Label = s.Key.ToString(),
                Matchups = s.Sum(c => c.Count),
                Wins = s.Sum(c => c.Mvp),
                duration = (long)s.Sum(c => c.Duration)
            }).ToList();


        return new StatsResponse()
        {
            Request = request,
            Items = data,
            CountResponse = await GetCount(request),
            AvgDuration = !data.Any() ? 0 : Convert.ToInt32(data.Select(s => s.duration / (double)s.Matchups).Average())
        };
    }

    public IQueryable<StatsResponseItem> GetStatsResponseItemQueryable(StatsRequest request, IQueryable<Replay> replays)
    {
        var toonIds = request.PlayerNames.Select(s => s.ToonId).ToList();

        return (request.Uploaders, request.Interest == Commander.None) switch
        {
            (false, true) => from r in replays
                             from p in r.ReplayPlayers
                             where toonIds.Contains(p.Player.ToonId)
                             group new { r, p } by new { race = p.Race } into g
                             select new StatsResponseItem()
                             {
                                 Label = g.Key.race.ToString(),
                                 Matchups = g.Count(),
                                 Wins = g.Count(c => c.p.Kills == c.r.Maxkillsum),
                                 duration = g.Sum(s => s.r.Duration)
                             },
            (false, false) => from r in replays
                              from p in r.ReplayPlayers
                              where p.Race == request.Interest && toonIds.Contains(p.Player.ToonId)
                              group new { r, p } by new { race = p.OppRace } into g
                              select new StatsResponseItem()
                              {
                                  Label = g.Key.race.ToString(),
                                  Matchups = g.Count(),
                                  Wins = g.Count(c => c.p.Kills == c.r.Maxkillsum),
                                  duration = g.Sum(s => s.r.Duration)
                              },
            (true, true) => from r in replays
                            from p in r.ReplayPlayers
                            where p.IsUploader && toonIds.Contains(p.Player.ToonId)
                            group new { r, p } by new { race = p.Race } into g
                            select new StatsResponseItem()
                            {
                                Label = g.Key.race.ToString(),
                                Matchups = g.Count(),
                                Wins = g.Count(c => c.p.Kills == c.r.Maxkillsum),
                                duration = g.Sum(s => s.r.Duration)
                            },
            (true, false) => from r in replays
                             from p in r.ReplayPlayers
                             where p.IsUploader && p.Race == request.Interest && toonIds.Contains(p.Player.ToonId)
                             group new { r, p } by new { race = p.OppRace } into g
                             select new StatsResponseItem()
                             {
                                 Label = g.Key.race.ToString(),
                                 Matchups = g.Count(),
                                 Wins = g.Count(c => c.p.Kills == c.r.Maxkillsum),
                                 duration = g.Sum(s => s.r.Duration)
                             },
        };
    }

    public async Task<StatsResponse> GetCustomMvp(StatsRequest request)
    {
        var replays = GetCustomRequestReplays(request);

        IQueryable<StatsResponseItem> responses;

        if (request.PlayerNames.Any())
        {
            responses = GetStatsResponseItemQueryable(request, replays);
        }
        else
        {
            responses = (request.Uploaders, request.Interest == Commander.None) switch
            {
                (false, true) => from r in replays
                                 from p in r.ReplayPlayers
                                 group new { r, p } by new { race = p.Race } into g
                                 select new StatsResponseItem()
                                 {
                                     Label = g.Key.race.ToString(),
                                     Matchups = g.Count(),
                                     Wins = g.Count(c => c.p.Kills == c.r.Maxkillsum),
                                     duration = g.Sum(s => s.r.Duration)
                                 },
                (false, false) => from r in replays
                                  from p in r.ReplayPlayers
                                  where p.Race == request.Interest
                                  group new { r, p } by new { race = p.OppRace } into g
                                  select new StatsResponseItem()
                                  {
                                      Label = g.Key.race.ToString(),
                                      Matchups = g.Count(),
                                      Wins = g.Count(c => c.p.Kills == c.r.Maxkillsum),
                                      duration = g.Sum(s => s.r.Duration)
                                  },
                (true, true) => from r in replays
                                from p in r.ReplayPlayers
                                where p.IsUploader
                                group new { r, p } by new { race = p.Race } into g
                                select new StatsResponseItem()
                                {
                                    Label = g.Key.race.ToString(),
                                    Matchups = g.Count(),
                                    Wins = g.Count(c => c.p.Kills == c.r.Maxkillsum),
                                    duration = g.Sum(s => s.r.Duration)
                                },
                (true, false) => from r in replays
                                 from p in r.ReplayPlayers
                                 where p.IsUploader && p.Race == request.Interest
                                 group new { r, p } by new { race = p.OppRace } into g
                                 select new StatsResponseItem()
                                 {
                                     Label = g.Key.race.ToString(),
                                     Matchups = g.Count(),
                                     Wins = g.Count(c => c.p.Kills == c.r.Maxkillsum),
                                     duration = g.Sum(s => s.r.Duration)
                                 },
            };
        }

        var items = await responses.ToListAsync();

        if (!items.Any())
        {
            return new StatsResponse()
            {
                Request = request,
                Items = new List<StatsResponseItem>()
            };
        }

        return new StatsResponse()
        {
            Request = request,
            Items = items,
            CountResponse = await GetCount(request),
            AvgDuration = !items.Any() ? 0 : Convert.ToInt32(items.Select(s => s.duration / (double)s.Matchups).Average())
        };
    }
}
