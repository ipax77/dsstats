using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using pax.dsstats.dbng.Extensions;
using pax.dsstats.shared;
using System.Text;

namespace pax.dsstats.dbng.Services;
public partial class StatsService
{
    public async Task<CountResponse> GetCount(StatsRequest request)
    {
        var memKey = request.GenCountMemKey();
        if (!memoryCache.TryGetValue(memKey, out CountResponse countResponse))
        {
            countResponse = await GetCountFromDb(request);
            memoryCache.Set(memKey, countResponse, new MemoryCacheEntryOptions()
            .SetPriority(CacheItemPriority.High)
            .SetAbsoluteExpiration(TimeSpan.FromDays(1)));
        }
        return countResponse;
    }

    private async Task<int> GetLeaver(StatsRequest request)
    {
        var defaultFilterGroup = context.GroupByHelpers.FromSqlRaw(
                        @$"SELECT r.Maxleaver > 89 as Name, count(*) AS Count
                            FROM Replays AS r
                            {GetRequestQueryString(request)}
                            GROUP BY r.Maxleaver > 89");

        var group = await defaultFilterGroup.ToListAsync();

        return group.FirstOrDefault(f => f.Group)?.Count ?? 0;
    }

    private async Task<int> GetQuits(StatsRequest request)
    {
        var defaultFilterGroup = context.GroupByHelpers.FromSqlRaw(
                        @$"SELECT r.WinnerTeam > 0 as Name, count(*) AS Count
                            FROM Replays AS r
                            {GetRequestQueryString(request)}
                            GROUP BY r.WinnerTeam > 0");

        var group = await defaultFilterGroup.ToListAsync();

        return group.FirstOrDefault(f => !f.Group)?.Count ?? 0;
    }

    private async Task<CountResponse> GetCountFromDb(StatsRequest request, bool details = false)
    {
        if (request.PlayerNames.Any())
        {
            return await GetPlayersCountFromDb(request);
        }

        var defaultFilterGroup = context.GroupByHelpers.FromSqlRaw(
                        @$"SELECT r.DefaultFilter AS Name, count(*) AS Count
                            FROM Replays AS r
                            {GetRequestQueryString(request)}
                            GROUP BY r.DefaultFilter");

        var group = await defaultFilterGroup.ToListAsync();

        int defaultReplays = group.FirstOrDefault(f => f.Group)?.Count ?? 0;
        int otherReplays = group.FirstOrDefault(f => !f.Group)?.Count ?? 0;

        var quits = await GetQuits(request);
        var leaver = await GetLeaver(request);

        return new CountResponse()
        {
            Count = defaultReplays + otherReplays,
            DefaultFilter = defaultReplays,
            Leaver = leaver,
            Quits = quits
        };
    }

    private string GetRequestQueryString(StatsRequest request)
    {
        (var startTime, var endTime) = Data.TimeperiodSelected(request.TimePeriod);

        StringBuilder sb = new();

        sb.Append($"WHERE r.GameTime > '{startTime.ToString(@"yyyy-MM-dd")}'");

        if (endTime < DateTime.UtcNow.Date.AddDays(-2))
        {
            sb.Append($" AND r.GameTime < '{endTime.ToString(@"yyyy-MM-dd")}'");
        }

        if (request.TeMaps)
        {
            sb.Append($" AND r.{nameof(Replay.TournamentEdition)} = 1");
        }

        if (request.GameModes.Any())
        {
            sb.Append($" AND r.GameMode IN ({string.Join(", ", request.GameModes.Select(s => (int)s))})");
        }

        if (request.Interest != Commander.None)
        {
            sb.Append($" AND EXISTS (SELECT 1 FROM ReplayPlayers AS rp WHERE r.ReplayId = rp.ReplayID AND rp.Race = {(int)request.Interest})");
        }

        // logger.LogWarning($"{request.TimePeriod} => {sb.ToString()}");
        return sb.ToString();
    }

    private async Task<CountResponse> GetPlayersCountFromDb(StatsRequest request)
    {
        var replays = GetCountReplaysQueryiable(request);

        var toonIds = request.PlayerNames.Select(s => s.ToonId).ToList();

        var playerReplays = from r in replays
                            from rp in r.ReplayPlayers
                            where toonIds.Contains(rp.Player.ToonId)
                            select r;

        var defaultGroup = from r in playerReplays
                           group r by r.DefaultFilter into g
                           select new GroupByHelper()
                           {
                               Group = g.Key,
                               Count = g.Count()
                           };

        var group = await defaultGroup.ToListAsync();

        int defaultReplays = group.FirstOrDefault(f => f.Group)?.Count ?? 0;
        int otherReplays = group.FirstOrDefault(f => !f.Group)?.Count ?? 0;

        var quits = await GetPlayersQuits(replays, toonIds);
        var leaver = await GetPlayersLeavers(replays, toonIds);

        return new CountResponse()
        {
            Count = defaultReplays + otherReplays,
            DefaultFilter = defaultReplays,
            Leaver = leaver,
            Quits = quits
        };
    }

    private async Task<int> GetPlayersQuits(IQueryable<Replay> replays, List<int> toonIds)
    {
        var quitGroup = from r in replays
                        from rp in r.ReplayPlayers
                        where toonIds.Contains(rp.Player.ToonId)
                        group r by r.WinnerTeam > 0 into g
                        select new GroupByHelper()
                        {
                            Group = g.Key,
                            Count = g.Count()
                        };
        var group = await quitGroup.ToListAsync();

        return group.FirstOrDefault(f => !f.Group)?.Count ?? 0;
    }

    private async Task<int> GetPlayersLeavers(IQueryable<Replay> replays, List<int> toonIds)
    {
        var leaverGroup = from r in replays
                          from rp in r.ReplayPlayers
                          where toonIds.Contains(rp.Player.ToonId)
                          group r by r.Maxleaver > 89 into g
                          select new GroupByHelper()
                          {
                              Group = g.Key,
                              Count = g.Count()
                          };

        var group = await leaverGroup.ToListAsync();
        return group.FirstOrDefault(f => f.Group)?.Count ?? 0;
    }

    private IQueryable<Replay> GetCountReplaysQueryiable(StatsRequest request)
    {
        (var startTime, var endTime) = Data.TimeperiodSelected(request.TimePeriod);

        var replays = context.Replays
            .Include(i => i.ReplayPlayers)
                .ThenInclude(i => i.Player)
            .Where(x => x.GameTime > startTime)
            .AsNoTracking();

        if (endTime < DateTime.UtcNow.Date.AddDays(-2))
        {
            replays = replays.Where(x => x.GameTime <= endTime);
        }

        if (request.TeMaps)
        {
            replays = replays.Where(x => x.TournamentEdition);
        }

        if (request.GameModes.Any())
        {
            replays = replays.Where(x => request.GameModes.Contains(x.GameMode));
        }

        return replays;
    }
}


