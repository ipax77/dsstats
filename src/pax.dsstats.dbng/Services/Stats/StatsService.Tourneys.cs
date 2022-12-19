
using Microsoft.EntityFrameworkCore;
using pax.dsstats.shared;

namespace pax.dsstats.dbng.Services;

public partial class StatsService
{
    public async Task<StatsResponse> GetTourneyStats(StatsRequest statsRequest, CancellationToken token)
    {
        var replays = GetTourneyReplays(statsRequest);

        var results = from r in replays
                      from rp in r.ReplayPlayers
                      group new { r, rp } by new { race = rp.Race } into g
                      select new StatsResponseItem
                      {
                          Label = g.Key.race.ToString(),
                          Matchups = g.Count(),
                          Wins = g.Count(c => c.rp.PlayerResult == PlayerResult.Win),
                          duration = g.Sum(s => s.r.Duration),
                      };

        var items = await results
            .AsNoTracking()
            .ToListAsync(token);

        var response = new StatsResponse()
        {
            Request = statsRequest,
            Items = items,
            Count = await replays.CountAsync(),
            AvgDuration = !items.Any() ? 0 : Convert.ToInt32(items.Select(s => s.duration / (double)s.Matchups).Average())
        };

        await SetBans(replays, response, token);

        return response;
    }

    private async Task SetBans(IQueryable<Replay> replays, StatsResponse response, CancellationToken token)
    {
        var events = replays.Select(s => s.ReplayEvent).Distinct();
        response.Bans = await events.CountAsync(token);

        var bans1 = from e in events
                    where (int)e.Ban1 > 3
                    group new { e } by new { ban = e.Ban1 } into g
                    select new
                    {
                        Ban = g.Key.ban,
                        Count = g.Count()
                    };

        var bans2 = from e in events
                    where (int)e.Ban2 > 3
                    group new { e } by new { ban = e.Ban2 } into g
                    select new
                    {
                        Ban = g.Key.ban,
                        Count = g.Count()
                    };

        var lbans1 = await bans1.ToListAsync(token);
        var lbans2 = await bans2.ToListAsync(token);

        foreach (var item in response.Items)
        {
            var b1 = lbans1.FirstOrDefault(f => f.Ban == item.Cmdr);
            var b2 = lbans2.FirstOrDefault(f => f.Ban == item.Cmdr);

            var bCount = b1?.Count ?? 0 + b2?.Count ?? 0;
            item.Bans = bCount;
        }
    }

    private IQueryable<Replay> GetTourneyReplays(StatsRequest request)
    {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
        var replays = context.Replays
            .Include(i => i.ReplayEvent)
                .ThenInclude(j => j.Event)
            .Include(i => i.ReplayPlayers)
            .Where(x => x.ReplayEvent != null)
            .AsNoTracking();
#pragma warning restore CS8602 // Dereference of a possibly null reference.

        if (!string.IsNullOrEmpty(request.Tournament))
        {
            replays = replays
                .Where(x => x.ReplayEvent != null
                    && x.ReplayEvent.Event.Name == request.Tournament);
        }

        if (!string.IsNullOrEmpty(request.Round))
        {
            replays = replays.Where(x => x.ReplayEvent != null && x.ReplayEvent.Round.StartsWith(request.Round));
        }

        return replays;
    }
}
