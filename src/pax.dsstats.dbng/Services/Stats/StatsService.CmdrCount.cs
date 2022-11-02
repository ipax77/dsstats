using Microsoft.EntityFrameworkCore;
using pax.dsstats.shared;

namespace pax.dsstats.dbng.Services;

public partial class StatsService
{
    private async Task<StatsResponse> GetCmdrsCount(StatsRequest request)
    {
        if (!request.DefaultFilter)
        {
            return await GetCustomWinrate(request);
        }

        var firstNotSecond = request.GameModes.Except(defaultGameModes).ToList();
        var secondNotFirst = defaultGameModes.Except(request.GameModes).ToList();

        if (firstNotSecond.Any() || secondNotFirst.Any())
        {
            return await GetCustomWinrate(request);
        }

        var cmdrstats = await GetRequestStats(request);

        DateTime endTime = request.EndTime == DateTime.MinValue ? DateTime.Today.AddDays(1) : request.EndTime;

        var stats = cmdrstats.Where(x => x.Time >= request.StartTime && x.Time <= endTime);

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

        var data = request.Interest == Commander.None
            ? stats.GroupBy(g => g.Race)
                .Select(s => new StatsResponseItem()
                {
                    Label = s.Key.ToString(),
                    Matchups = s.Sum(c => c.Count),
                    duration = (long)s.Sum(c => c.Duration)
                }).ToList()
            : stats.GroupBy(g => g.OppRace)
                .Select(s => new StatsResponseItem()
                {
                    Label = s.Key.ToString(),
                    Matchups = s.Sum(c => c.Count),
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

    private async Task<StatsResponse> GetCustomCmdrsCount(StatsRequest request)
    {
        var replays = GetCustomRequestReplays(request);

        var group = request.Interest == Commander.None ?
            from r in replays
            from rp in r.ReplayPlayers
            group new { r, rp } by rp.Race into g
            select new StatsResponseItem()
            {
                Label = g.Key.ToString(),
                Matchups = g.Count(),
                duration = g.Sum(s => s.r.Duration)
            }
            :
            from r in replays
            from rp in r.ReplayPlayers
            where rp.Race == request.Interest
            group new { r, rp } by rp.Race into g
            select new StatsResponseItem()
            {
                Label = g.Key.ToString(),
                Matchups = g.Count(),
                duration = g.Sum(s => s.r.Duration)
            };

        var data = await group.ToListAsync();

        return new StatsResponse()
        {
            Request = request,
            Items = data,
            CountResponse = await GetCount(request),
            AvgDuration = !data.Any() ? 0 : Convert.ToInt32(data.Select(s => s.duration / (double)s.Matchups).Average())
        };

    }
}
