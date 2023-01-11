using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using pax.dsstats.shared;
using System.Diagnostics;

namespace pax.dsstats.dbng.Repositories;

public interface IStatsResponse
{

}

public record WinrateResult(StatsResponse StatsResponse) : IStatsResponse;
public record FailedResponse : IStatsResponse;

public partial class StatsRepository : IStatsRepository
{
    private readonly ILogger<StatsRepository> logger;
    private readonly ReplayContext context;
    private readonly IMapper mapper;

    public StatsRepository(ILogger<StatsRepository> logger, ReplayContext context, IMapper mapper)
    {
        this.logger = logger;
        this.context = context;
        this.mapper = mapper;
    }

    public async Task<IStatsResponse> GetStats(StatsRequest request, CancellationToken token = default)
    {
        return request.StatsMode switch
        {
            StatsMode.Winrate => await GetWinrate(request, token),
            _ => new FailedResponse()
        };
    }

    private async Task<IStatsResponse> GetWinrate(StatsRequest request, CancellationToken token)
    {
        Stopwatch sw = new();
        sw.Start();

        var replays = GetReplays(request);

        var results = from r in replays
                      from p in r.ReplayPlayers
                      group new { r, p } by new { race = p.Race } into g
                      select new StatsResponseItem
                      {
                          Label = g.Key.race.ToString(),
                          Matchups = g.Count(),
                          Wins = g.Count(c => c.p.PlayerResult == PlayerResult.Win),
                          duration = g.Sum(s => s.r.Duration),
                          // Replays = g.Select(s => s.r.ReplayId).Distinct().Count()
                      };

        var items = await results
            .AsNoTracking()
            .ToListAsync(token);

        sw.Stop();
        logger.LogInformation($"got winrate groups in {sw.ElapsedMilliseconds} ms");

        sw.Start();


        var response = new StatsResponse()
        {
            Request = request,
            Items = items,
            AvgDuration = !items.Any() ? 0 : Convert.ToInt32(items.Select(s => s.duration / (double)s.Matchups).Average())
        };

        await SetBans(replays, response, token);

        sw.Stop();
        logger.LogInformation($"got winrate result in {sw.ElapsedMilliseconds} ms");

        return new WinrateResult(response);
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

    private async Task<int> GetCount(IQueryable<Replay> replays)
    {
        return await replays.CountAsync();
    }


    private IQueryable<Replay> GetReplays(StatsRequest request)
    {
        IQueryable<Replay> replays;

        (var startTime, var endTime) = Data.TimeperiodSelected(request.TimePeriod);

        if (!String.IsNullOrEmpty(request.Tournament) || !String.IsNullOrEmpty(request.Round))
        {
#pragma warning disable CS8602 // Dereference of a possibly null reference. (ef core handles this https://github.com/dotnet/efcore/issues/17212)
            replays = context.Replays
                .Include(i => i.ReplayEvent)
                    .ThenInclude(j => j.Event)
                .Include(i => i.ReplayPlayers)
                .Where(x => x.GameTime > startTime);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
        }
        else
        {
            replays = context.Replays
                .Include(i => i.ReplayEvent)
                .Include(i => i.ReplayPlayers)
                .Where(x => x.GameTime > startTime);
        }

        if (endTime > DateTime.MinValue)
        {
            replays = replays.Where(x => x.GameTime < endTime);
        }

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


