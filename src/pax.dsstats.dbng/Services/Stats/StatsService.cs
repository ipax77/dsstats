using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using pax.dsstats.dbng.Extensions;
using pax.dsstats.dbng.Repositories;
using pax.dsstats.shared;
using System.Collections;
using System.Reflection;

namespace pax.dsstats.dbng.Services;

public partial class StatsService : IStatsService
{
    private readonly IMemoryCache memoryCache;
    private readonly ReplayContext context;
    private readonly IMapper mapper;
    private readonly IRatingRepository ratingRepository;
    private readonly IReplayRepository replayRepository;
    private readonly IOptions<DbImportOptions> dbImportOptions;
    private readonly ILogger<StatsService> logger;

    public StatsService(IMemoryCache memoryCache,
                        ReplayContext context,
                        IMapper mapper,
                        IRatingRepository ratingRepository,
                        IReplayRepository replayRepository,
                        IOptions<DbImportOptions> dbImportOptions,
                        ILogger<StatsService> logger)
    {
        this.memoryCache = memoryCache;
        this.context = context;
        this.mapper = mapper;
        this.ratingRepository = ratingRepository;
        this.replayRepository = replayRepository;
        this.dbImportOptions = dbImportOptions;
        this.logger = logger;
    }

    public async Task<StatsResponse> GetStatsResponse(StatsRequest request)
    {
        var memKey = request.GenStatsMemKey();

        if (!memoryCache.TryGetValue(memKey, out StatsResponse response))
        {
            response = request.StatsMode switch
            {
                StatsMode.Winrate => await GetWinrate(request),
                StatsMode.Timeline => await GetTimeline(request),
                StatsMode.Mvp => await GetMvp(request),
                StatsMode.Synergy => await GetSynergy(request),
                StatsMode.Count => await GetWinrate(request),
                StatsMode.Duration => await GetDuration(request),
                _ => new()
            };

            memoryCache.Set(memKey, response, new MemoryCacheEntryOptions()
                .SetPriority(CacheItemPriority.Low)
                .SetAbsoluteExpiration(TimeSpan.FromDays(7))
            );
        }
        return response;
    }

    public void ResetStatsCache()
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
                    var memkey = val.ToString();
                    if (memkey != null && memkey.StartsWith("Stats"))
                    {
                        items.Add(memkey);
                    }
                }
            }
        }
        items.ForEach(f => memoryCache.Remove(f));
    }

    public async Task<List<CmdrStats>> GetRequestStats(StatsRequest request)
    {
        string memKey = request.Uploaders ? "StatsCmdrUploaders" : "StatsCmdr";
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
                    from p in r.ReplayPlayers
                    where (r.GameMode == GameMode.Commanders || r.GameMode == GameMode.CommandersHeroic) && r.DefaultFilter && p.IsUploader
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
        return FilterStats(await stats.ToListAsync());
    }

    private async Task<List<CmdrStats>> GetStats()
    {
        var stats = from r in context.Replays
                    from p in r.ReplayPlayers
                    where (r.GameMode == GameMode.Commanders || r.GameMode == GameMode.CommandersHeroic) && r.DefaultFilter
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
        return FilterStats(await stats.ToListAsync());
    }

    private List<CmdrStats> FilterStats(List<CmdrStats> stats)
    {
        var cmdrs = Data.GetCommanders(Data.CmdrGet.NoStd);
        return stats.Where(x => cmdrs.Contains(x.Race) && cmdrs.Contains(x.OppRace)).ToList();
    }

    private IQueryable<Replay> GetCustomRequestReplays(StatsRequest request)
    {
        IQueryable<Replay> replays;

        (var startTime, var endTime) = Data.TimeperiodSelected(request.TimePeriod);

        if (request.PlayerNames.Any())
        {
            replays = context.Replays
                .Include(i => i.ReplayPlayers)
                    .ThenInclude(i => i.Player)
                .Where(x => x.GameTime > startTime)
                .AsNoTracking();
        }
        else
        {
            replays = context.Replays
                    .Include(i => i.ReplayPlayers)
                    .Where(x => x.GameTime > startTime)
                    .AsNoTracking();
        }

        if (endTime < DateTime.UtcNow.Date.AddDays(-2))
        {
            replays = replays.Where(x => x.GameTime <= endTime);
        }

        if (request.GameModes.Any())
        {
            replays = replays.Where(x => request.GameModes.Contains(x.GameMode));
        }

        if (request.TeMaps)
        {
            replays = replays.Where(x => x.TournamentEdition);
        }

        if (request.DefaultFilter)
        {
            replays = replays.Where(x => x.Duration > 300
                && x.Maxleaver < 90
                && x.Playercount == 6
                && x.WinnerTeam > 0);
        }
        else if (request.PlayerCount > 0)
        {
            replays = replays.Where(x => x.Playercount == request.PlayerCount);
        }

        return replays;
    }

    private IQueryable<ReplayPlayer> GetCustomRequestReplayPlayers(StatsRequest request)
    {
        IQueryable<Replay> replays;

        (var startTime, var endTime) = Data.TimeperiodSelected(request.TimePeriod);

        if (request.PlayerNames.Any())
        {
            replays = context.Replays
                    .Include(i => i.ReplayPlayers)
                        .ThenInclude(i => i.Player)
                    .Where(x => x.GameTime > startTime)
                    .AsNoTracking();
        }
        else
        {
            replays = context.Replays
                    .Include(i => i.ReplayPlayers)
                    .Where(x => x.GameTime > startTime)
                    .AsNoTracking();
        }

        if (endTime < DateTime.UtcNow.Date.AddDays(-2))
        {
            replays = replays.Where(x => x.GameTime <= endTime);
        }

        if (request.GameModes.Any())
        {
            replays = replays.Where(x => request.GameModes.Contains(x.GameMode));
        }

        if (request.DefaultFilter)
        {
            replays = replays.Where(x => x.Duration > 300
                && x.Maxleaver < 90
                && x.Playercount == 6
                && x.WinnerTeam > 0);
        }
        else if (request.PlayerCount > 0)
        {
            replays = replays.Where(x => x.Playercount == request.PlayerCount);
        }

        if (request.TeMaps)
        {
            replays = replays.Where(x => x.TournamentEdition);
        }

        var players = replays.SelectMany(s => s.ReplayPlayers);

        if (request.Uploaders)
        {
            players = players.Where(x => x.IsUploader);
        }

        if (request.Interest != Commander.None)
        {
            players = players.Where(x => x.Race == request.Interest);
        }

        if (request.PlayerNames.Any())
        {
            var toonIds = request.PlayerNames.Select(s => s.ToonId).ToList();
            players = players.Where(x => toonIds.Contains(x.Player.ToonId));
        }

        return players;
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