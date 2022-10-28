using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using pax.dsstats.shared;
using System.Text;

namespace pax.dsstats.dbng.Services;
public partial class StatsService
{
    private async Task<(int, int)> GetRequestCount(StatsRequest request)
    {
        var memkey = GetRequestHash(request);
        if (!memoryCache.TryGetValue(memkey, out (int, int) counts))
        {
            counts = await GetCount(request);
            memoryCache.Set(memkey, counts, new MemoryCacheEntryOptions()
                .SetPriority(CacheItemPriority.High)
                .SetAbsoluteExpiration(TimeSpan.FromDays(1))
            );
        }
        return counts;
    }

    private async Task<double> GetLeaver(StatsRequest request)
    {
        var replays = GetCountReplays(request);

        var leaver = from r in replays
                     group r by r.Maxleaver > 89 into g
                     select new
                     {
                         Leaver = g.Key,
                         Count = g.Count()
                     };
        var lleaver = await leaver.ToListAsync();
        if (lleaver.Any() && lleaver.First().Count > 0)
            return Math.Round(lleaver.Last().Count / (double)lleaver.First().Count * 100, 2);
        else
            return 0;
    }

    private async Task<double> GetQuits(StatsRequest request)
    {
        var replays = GetCountReplays(request);

        var quits = from r in replays
                    group r by r.WinnerTeam into g
                    select new
                    {
                        Winner = g.Key,
                        Count = g.Count()
                    };
        var lquits = await quits.ToListAsync();
        if (lquits.Any())
        {
            double sum = (double)lquits.Sum(s => s.Count);
            if (sum > 0)
                return Math.Round(lquits.Where(x => x.Winner == -1).Count() * 100 / sum, 2);
            else
                return 0;
        }
        else
            return 0;
    }

    private async Task<(int, int)> GetCount(StatsRequest request, bool details = false)
    {
        var replays = GetCountReplays(request);

        var count = from r in replays
                    group r by r.DefaultFilter into g
                    select new
                    {
                        DefaultFilter = g.Key,
                        Count = g.Count()
                    };

        var lcount = await count
            .ToListAsync();

        return (lcount.FirstOrDefault(f => !f.DefaultFilter)?.Count ?? 0, lcount.FirstOrDefault(f => f.DefaultFilter)?.Count ?? 0);
    }

    private IQueryable<Replay> GetCountReplays(StatsRequest request)
    {
        var replays = context.Replays
                .Include(i => i.ReplayPlayers)
                .Where(x => x.GameTime > request.StartTime)
                .AsNoTracking();

        if (request.EndTime != DateTime.Today)
        {
            replays = replays.Where(x => x.GameTime <= request.EndTime);
        }

        if (request.GameModes.Any())
        {
            replays = replays.Where(x => request.GameModes.Contains(x.GameMode));
        }

        if (request.Interest != Commander.None)
        {
            if (request.Versus != Commander.None)
            {
                replays = replays.Where(x => x.ReplayPlayers.Any(a => a.Race == request.Interest && a.OppRace == request.Versus));
            }
            else
            {
                replays = replays.Where(x => x.ReplayPlayers.Any(a => a.Race == request.Interest));
            }
        }

        if (request.PlayerNames.Any())
        {
            replays = replays.Where(x => x.ReplayPlayers.Any(a => request.PlayerNames.Contains(a.Name)));
        }

        if (request.PlayerCount > 0)
        {
            replays = replays.Where(x => x.Playercount == request.PlayerCount);
        }

        return replays;
    }

    private string GetRequestHash(StatsRequest request)
    {
        StringBuilder sb = new();
        sb.Append("StatsRequest");
        sb.Append(request.StartTime.ToString());
        sb.Append(request.EndTime.ToString());
        sb.Append(request.Interest.ToString());
        sb.Append(request.Versus.ToString());
        sb.Append(request.Uploaders.ToString());
        sb.Append(request.DefaultFilter.ToString());
        sb.Append(request.PlayerCount.ToString());
        sb.Append(String.Concat(request.PlayerNames));
        sb.Append(String.Concat(request.GameModes.ToString()));
        //sb.Append(request.Tournament);
        //sb.Append(request.Round);
        return sb.ToString();
    }
}
