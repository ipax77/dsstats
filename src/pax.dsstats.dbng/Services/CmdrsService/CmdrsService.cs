using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using pax.dsstats.dbng.Extensions;
using pax.dsstats.shared;

namespace pax.dsstats.dbng.Services;

public partial class CmdrsService
{
    private readonly ReplayContext context;
    private readonly IStatsService statsService;
    private readonly IRatingRepository ratingRepository;
    private readonly IMemoryCache memoryCache;

    public CmdrsService(ReplayContext context, IStatsService statsService, IRatingRepository ratingRepository, IMemoryCache memoryCache)
    {
        this.context = context;
        this.statsService = statsService;
        this.ratingRepository = ratingRepository;
        this.memoryCache = memoryCache;
    }

    public async Task<CmdrResult> GetCmdrInfo(CmdrRequest request, CancellationToken token = default)
    {
        var memKey = request.GenMemKey();
        if (!memoryCache.TryGetValue(memKey, out CmdrResult cmdrResult))
        {
            cmdrResult = await GetCmdrInfoFromDb(request, token);
            memoryCache.Set(memKey, cmdrResult, new MemoryCacheEntryOptions()
                .SetPriority(CacheItemPriority.Low)
                .SetAbsoluteExpiration(TimeSpan.FromDays(7))
            );
        };
        return cmdrResult;
    }

    private async Task<CmdrResult> GetCmdrInfoFromDb(CmdrRequest cmdrRequest, CancellationToken token)
    {
        List<GameMode> gameModes;
        if ((int)cmdrRequest.Cmdr > 3)
        {
            gameModes = new List<GameMode>() { GameMode.Commanders, GameMode.CommandersHeroic };
        }
        else
        {
            gameModes = new List<GameMode>() { GameMode.Standard };
        }

        if (token.IsCancellationRequested)
        {
            throw new OperationCanceledException();
        }

        var winrate = await statsService.GetStatsResponse(new()
        {
            StatsMode = StatsMode.Winrate,
            Interest = cmdrRequest.Cmdr,
            TimePeriod = cmdrRequest.TimeSpan,
            DefaultFilter = true,
            GameModes = gameModes,
            Uploaders = cmdrRequest.Uploaders
        });

        if (token.IsCancellationRequested)
        {
            throw new OperationCanceledException();
        }

        var count = await statsService.GetStatsResponse(new()
        {
            StatsMode = StatsMode.Count,
            Interest = Commander.None,
            TimePeriod = cmdrRequest.TimeSpan,
            DefaultFilter = true,
            GameModes = gameModes,
            Uploaders = cmdrRequest.Uploaders
        });

        if (token.IsCancellationRequested)
        {
            throw new OperationCanceledException();
        }

        var duration = await statsService.GetStatsResponse(new()
        {
            StatsMode = StatsMode.Duration,
            Interest = cmdrRequest.Cmdr,
            TimePeriod = cmdrRequest.TimeSpan,
            DefaultFilter = true,
            GameModes = gameModes,
            Uploaders = cmdrRequest.Uploaders
        });

        if (token.IsCancellationRequested)
        {
            throw new OperationCanceledException();
        }

        //var timeline = await statsService.GetStatsResponse(new()
        //{
        //    StatsMode = StatsMode.Timeline,
        //    Interest = cmdrRequest.Cmdr,
        //    StartTime = startTime,
        //    EndTime = endTime,
        //    DefaultFilter = true,
        //    Uploaders = cmdrRequest.Uploaders
        //});

        var synergy = await statsService.GetStatsResponse(new()
        {
            StatsMode = StatsMode.Synergy,
            Interest = cmdrRequest.Cmdr,
            TimePeriod = cmdrRequest.TimeSpan,
            DefaultFilter = true,
            GameModes = gameModes,
            Uploaders = cmdrRequest.Uploaders
        });

        if (token.IsCancellationRequested)
        {
            throw new OperationCanceledException();
        }

        int countSum = count.Items.Sum(s => s.Matchups);
        int countCmdr = count.Items.FirstOrDefault(f => f.Label == cmdrRequest.Cmdr.ToString())?.Matchups ?? 0;
        float countPer = countSum == 0 ? 0 : MathF.Round(countCmdr * 100.0f / countSum, 2);
        var bestDur = duration.Items.OrderByDescending(o => o.Winrate).FirstOrDefault();
        var bestSynergy = synergy.Items.OrderByDescending(o => o.Winrate).FirstOrDefault();
        var worstSynergy = synergy.Items.OrderByDescending(o => o.Winrate).LastOrDefault();

        var cmdrResult = new CmdrResult()
        {
            Cmdr = cmdrRequest.Cmdr,
            Played = new()
            {
                Matchups = countCmdr,
                Per = countPer
            },
            Winrate = MathF.Round(winrate.Items.Sum(s => s.Wins) * 100.0f / winrate.Items.Sum(s => s.Matchups), 2),
            AvgDuration = winrate.AvgDuration,
            BestMatchup = winrate.Items.OrderBy(o => o.Winrate).LastOrDefault(),
            WorstMatchup = winrate.Items.OrderBy(o => o.Winrate).FirstOrDefault(),
            BestDuration = new CmdrDuration()
            {
                Dur = bestDur?.Label ?? "",
                Wr = bestDur == null || bestDur.Matchups == 0 ? 0 : MathF.Round(bestDur.Wins * 100.0f / bestDur.Matchups, 2)
            },
            BestSynergy = new CmdrSynergy()
            {
                Cmdr = bestSynergy?.Cmdr ?? Commander.None,
                Wr = bestSynergy == null ? 0 : MathF.Round((float)bestSynergy.Winrate, 2)
            },
            WorstSynergy = new CmdrSynergy()
            {
                Cmdr = worstSynergy?.Cmdr ?? Commander.None,
                Wr = worstSynergy == null ? 0 : MathF.Round((float)worstSynergy.Winrate, 2)
            },
            TopPlayers = await GetBestPayers(cmdrRequest, token),
        };

        return cmdrResult;
    }

    private async Task<List<CmdrTopPlayer>> GetBestPayers(CmdrRequest cmdrRequest, CancellationToken token)
    {
        var gameModes = (int)cmdrRequest.Cmdr > 3 ?
            new List<GameMode>() { GameMode.Commanders, GameMode.CommandersHeroic }
            : new List<GameMode>() { GameMode.Standard };

        (var startTime, var endTime) = Data.TimeperiodSelected(cmdrRequest.TimeSpan);

        var replayPlayersQuery = cmdrRequest.Uploaders ?
                                from r in context.Replays
                                from rp in r.ReplayPlayers
                                where r.DefaultFilter
                                   && gameModes.Contains(r.GameMode)
                                   && r.GameTime > startTime
                                   && r.GameTime < endTime.AddDays(1)
                                   && rp.IsUploader
                                   && rp.Race == cmdrRequest.Cmdr
                                select rp
                            : from r in context.Replays
                              from rp in r.ReplayPlayers
                              where r.DefaultFilter
                                 && gameModes.Contains(r.GameMode)
                                 && r.GameTime > startTime
                                 && r.GameTime < endTime.AddDays(1)
                                 && rp.Race == cmdrRequest.Cmdr
                              select rp;

        var limit = (endTime - startTime).TotalDays switch
        {
            < 10 => 10,
            < 20 => 20,
            < 30 => 30,
            < 40 => 40,
            _ => 60
        };

        var bestPlayersQuery = from rp in replayPlayersQuery
                               group new { rp, rp.Player } by rp.Player.ToonId into g
                               where g.Count() > limit
                               orderby g.Count(c => c.rp.PlayerResult == PlayerResult.Win) / g.Count() descending
                               select new CmdrTopPlayer
                               {
                                   ToonId = g.Key,
                                   Count = g.Count(),
                                   Wins = g.Count(c => c.rp.PlayerResult == PlayerResult.Win)
                               };

        var bestPlayers = await bestPlayersQuery
            .Take(5)
            .ToListAsync(token);

        foreach (var bestPlayer in bestPlayers)
        {
            bestPlayer.Name = await ratingRepository.GetToonIdName(bestPlayer.ToonId) ?? "Anonymous";
        }

        return bestPlayers;
    }
}

