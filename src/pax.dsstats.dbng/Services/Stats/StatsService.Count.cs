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
        var memKey = request.GenMemKey();
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
        StringBuilder sb = new();

        sb.Append($"WHERE r.GameTime > '{request.StartTime.ToString(@"yyyy-MM-dd")}'");

        if (request.EndTime != DateTime.Today)
        {
            sb.Append($" AND r.GameTime < '{request.EndTime.ToString(@"yyyy-MM-dd")}'");
        }

        if (request.GameModes.Any())
        {
            sb.Append($" AND r.GameMode IN ({string.Join(", ", request.GameModes.Select(s => (int)s))})");
        }

        if (request.Interest != Commander.None)
        {
            sb.Append($" AND EXISTS (SELECT 1 FROM ReplayPlayers AS rp WHERE r.ReplayId = rp.ReplayID AND rp.Race = {(int)request.Interest})");
        }

        return sb.ToString();
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
}


