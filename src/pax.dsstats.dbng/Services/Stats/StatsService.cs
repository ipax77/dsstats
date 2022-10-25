using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using pax.dsstats.shared;
using System.Collections;
using System.Reflection;

namespace pax.dsstats.dbng.Services;

public partial class StatsService : IStatsService
{
    private readonly IMemoryCache memoryCache;
    private readonly ReplayContext context;
    private readonly IMapper mapper;

    public StatsService(IMemoryCache memoryCache, ReplayContext context, IMapper mapper)
    {
        this.memoryCache = memoryCache;
        this.context = context;
        this.mapper = mapper;
    }

    public async Task<StatsResponse> GetStatsResponse(StatsRequest request)
    {
        return request.StatsMode switch
        {
            StatsMode.Winrate => await GetWinrate(request),
            StatsMode.Timeline => await GetTimeline(request),
            _ => new()
        };
    }

    public void ResetCache()
    {
        var field = typeof(MemoryCache).GetProperty("EntriesCollection", BindingFlags.NonPublic | BindingFlags.Instance);
        var collection = field?.GetValue(memoryCache) as ICollection;
        var items = new List<string>();
        if (collection != null)
        {
            foreach (var item in collection)
            {
                var methodInfo = item.GetType().GetProperty("Key");
                var val = methodInfo?.GetValue(item);
                if (val != null)
                {
                    items.Add(val.ToString() ?? "");
                }
            }
        }
        items.ForEach(f => memoryCache.Remove(f));
    }

    private async Task<List<CmdrStats>> GetRequestStats(StatsRequest request)
    {
        string memKey = request.Uploaders ? "cmdrstatsuploaders" : "cmdrstats";
        if (!memoryCache.TryGetValue(memKey, out List<CmdrStats> stats))
        {
            if (request.Uploaders)
            {
                stats = await GetUploaderStats();
            }
            else
            {
                stats = await GetStats();
            }
            memoryCache.Set(memKey, stats, new MemoryCacheEntryOptions()
                .SetPriority(CacheItemPriority.High)
                .SetAbsoluteExpiration(TimeSpan.FromDays(1))
            );
        }
        return stats;
    }

    private async Task<List<CmdrStats>> GetUploaderStats()
    {
        var stats = from r in context.Replays
                    from p in r.Players
                    where p.IsUploader
                    group new { r, p } by new { year = r.GameTime.Year, month = r.GameTime.Month, race = p.Race, opprace = p.OppRace } into g
                    select new CmdrStats()
                    {
                        Year = g.Key.year,
                        Month = g.Key.month,
                        Time = new DateTime(g.Key.year, g.Key.month, 1),
                        Race = g.Key.race,
                        OppRace = g.Key.opprace,
                        Count = g.Count(),
                        Wins = g.Count(c => c.p.PlayerResult == PlayerResult.Win),
                        Mvp = g.Count(c => c.p.Kills == c.r.Maxkillsum),
                        Army = g.Sum(s => s.p.Army),
                        Kills = g.Sum(s => s.p.Kills),
                        Duration = g.Sum(s => s.r.Duration),
                    };
        return await stats.ToListAsync();
    }

    private async Task<List<CmdrStats>> GetStats()
    {
        var stats = from r in context.Replays
                    from p in r.Players
                    where r.GameMode == GameMode.Commanders || r.GameMode == GameMode.CommandersHeroic
                    // where r.DefaultFilter && p.IsUploader
                    group new { r, p } by new { year = r.GameTime.Year, month = r.GameTime.Month, race = p.Race, opprace = p.OppRace } into g
                    select new CmdrStats()
                    {
                        Year = g.Key.year,
                        Month = g.Key.month,
                        Time = new DateTime(g.Key.year, g.Key.month, 1),
                        Race = g.Key.race,
                        OppRace = g.Key.opprace,
                        Count = g.Count(),
                        Wins = g.Count(c => c.p.PlayerResult == PlayerResult.Win),
                        Mvp = g.Count(c => c.p.Kills == c.r.Maxkillsum),
                        Army = g.Sum(s => s.p.Army),
                        Kills = g.Sum(s => s.p.Kills),
                        Duration = g.Sum(s => s.r.Duration),
                    };
        return await stats.ToListAsync();
    }



}

public record CmdrStats
{
    public int Year { get; init; }
    public int Month { get; init; }
    public DateTime Time { get; init; }
    public Commander Race { get; init; }
    public Commander OppRace { get; init; }
    public int Count { get; init; }
    public int Wins { get; init; }
    public int Mvp { get; init; }
    public decimal Army { get; init; }
    public decimal Kills { get; init; }
    public decimal Duration { get; init; }
    public int Replays { get; init; }
}