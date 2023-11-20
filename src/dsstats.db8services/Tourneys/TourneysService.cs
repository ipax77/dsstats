using dsstats.db8;
using dsstats.shared;
using dsstats.shared.Extensions;
using dsstats.shared.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace dsstats.db8services;

public class TourneysService(ReplayContext context) : ITourneysService
{
    public async Task<List<TourneyDto>> GetTourneys()
    {
        return await context.Events
            .Select(s => new TourneyDto()
            {
                Name = s.Name,
                EventGuid = s.EventGuid,
                EventStart = s.EventStart,
                GameMode = s.GameMode,
                WinnerTeam = s.WinnerTeam
            }).ToListAsync();
    }

    public async Task<int> GetReplaysCount(TourneysReplaysRequest request, CancellationToken token = default)
    {
        var replays = GetReplaysQueriable(request);
        return await replays.CountAsync(token);
    }

    public async Task<List<TourneysReplayListDto>> GetReplays(TourneysReplaysRequest request, CancellationToken token)
    {
        if (request.Take <= 0)
        {
            return new();
        }

        var replays = GetReplaysQueriable(request);
        var list = GetReplaysList(replays);
        list = SortReplays(request, list);

        return await list
            .Skip(request.Skip)
            .Take(request.Take)
            .ToListAsync();
    }

    private IQueryable<TourneysReplayListDto> SortReplays(TourneysReplaysRequest request, IQueryable<TourneysReplayListDto> replays)
    {
        if (request.Orders.Count == 0)
        {
            return replays.OrderByDescending(o => o.GameTime);
        }

        foreach (var order in request.Orders)
        {
            if (order.Ascending)
            {
                replays = replays.AppendOrderBy(order.Property);
            }
            else
            {
                replays = replays.AppendOrderByDescending(order.Property);
            }
        }
        return replays;
    }

    private IQueryable<TourneysReplayListDto> GetReplaysList(IQueryable<Replay> replays)
    {
        return replays
            .Select(s => new TourneysReplayListDto()
            {
                GameTime = s.GameTime,
                Duration = s.Duration,
                WinnerTeam = s.WinnerTeam,
                TournamentEdition = s.TournamentEdition,
                ReplayHash = s.ReplayHash,
                DefaultFilter = s.DefaultFilter,
                CommandersTeam1 = s.CommandersTeam1,
                CommandersTeam2 = s.CommandersTeam2,
                MaxLeaver = s.Maxleaver,
                ReplayEvent = new ReplayEventDto()
                {
                    Round = s.ReplayEvent!.Round,
                    WinnerTeam = s.ReplayEvent.WinnerTeam,
                    RunnerTeam = s.ReplayEvent.RunnerTeam,
                    Ban1 = s.ReplayEvent.Ban1,
                    Ban2 = s.ReplayEvent.Ban2,
                    Ban3 = s.ReplayEvent.Ban3,
                    Ban4 = s.ReplayEvent.Ban4,
                    Ban5 = s.ReplayEvent.Ban5,
                    Event = new EventDto()
                    {
                        Name = s.ReplayEvent.Event.Name
                    }
                }
            });
    }

    private IQueryable<Replay> GetReplaysQueriable(TourneysReplaysRequest request)
    {
        var replays = context.Replays
            .Where(x => x.GameTime > new DateTime(2018, 1, 1)
                && x.ReplayEvent != null
                && x.ReplayEvent.Event != null
                && (request.EventGuid == Guid.Empty || x.ReplayEvent!.Event!.EventGuid == request.EventGuid));

        if (!string.IsNullOrEmpty(request.Tournament))
        {
            replays = replays.Where(x => x.ReplayEvent!.Event.Name == request.Tournament);
        }

        return replays;
    }

    public async Task<TourneysStatsResponse> GetTourneyStats(TourneysStatsRequest statsRequest, CancellationToken token)
    {
        var replays = GetTourneyReplays(statsRequest);

        var results = from r in replays
                      from rp in r.ReplayPlayers
                      group new { r, rp } by new { race = rp.Race } into g
                      select new TourneysStatsResponseItem
                      {
                          Label = g.Key.race.ToString(),
                          Matchups = g.Count(),
                          Wins = g.Count(c => c.rp.PlayerResult == PlayerResult.Win),
                          duration = g.Sum(s => s.r.Duration),
                      };

        var items = await results
            .AsNoTracking()
            .ToListAsync(token);

        var response = new TourneysStatsResponse()
        {
            Request = statsRequest,
            Items = items,
            Count = await replays.CountAsync(),
            AvgDuration = !items.Any() ? 0 : Convert.ToInt32(items.Select(s => s.duration / (double)s.Matchups).Average())
        };

        await SetBans(replays, response, token);

        return response;
    }

    private async Task SetBans(IQueryable<Replay> replays, TourneysStatsResponse response, CancellationToken token)
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

    private IQueryable<Replay> GetTourneyReplays(TourneysStatsRequest request)
    {
        var replays = context.Replays
            .Include(i => i.ReplayEvent!)
                .ThenInclude(j => j.Event)
            .Include(i => i.ReplayPlayers)
            .Where(x => x.ReplayEvent != null)
            .AsNoTracking();

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

    public async Task<(string, string)?> DownloadReplay(string replayHash)
    {
        var replay = await context.Replays
            .Include(i => i.ReplayEvent)
            .Where(x => x.ReplayHash == replayHash)
            .FirstOrDefaultAsync();

        if (replay is null || string.IsNullOrEmpty(replay.FileName)
            || !File.Exists(replay.FileName))
        {
            return null;
        }

        context.ReplayDownloadCounts.Add(new() { ReplayHash = replayHash });
        await context.SaveChangesAsync();

        var fileName = $"{replay.ReplayEvent?.WinnerTeam?.Replace(" ", "_") ?? "Team1"}_vs_{replay.ReplayEvent?.RunnerTeam?.Replace(" ", "_") ?? "Team2"}.SC2Replay";

        return (replay.FileName, fileName);
    }
}
