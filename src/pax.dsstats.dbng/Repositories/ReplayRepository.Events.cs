using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using pax.dsstats.dbng.Extensions;
using pax.dsstats.shared;

namespace pax.dsstats.dbng.Repositories;

public partial class ReplayRepository : IReplayRepository
{
    public async Task<int> GetEventReplaysCount(ReplaysRequest request, CancellationToken token = default)
    {
        var replays = GetRequestEventReplays(request);
        return await replays.CountAsync(token);
    }

    public async Task<ICollection<ReplayListEventDto>> GetEventReplays(ReplaysRequest request, CancellationToken token = default)
    {
        var replays = GetRequestEventReplays(request);

        replays = SortEventReplays(request, replays);

        if (token.IsCancellationRequested)
        {
            return new List<ReplayListEventDto>();
        }

        var list = await replays
            .Skip(request.Skip)
            .Take(request.Take)
            .AsNoTracking()
            .ProjectTo<ReplayListEventDto>(mapper.ConfigurationProvider)
            .ToListAsync(token);

        return list;
    }

    private IQueryable<Replay> SortEventReplays(ReplaysRequest request, IQueryable<Replay> replays)
    {

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

    private IQueryable<Replay> GetRequestEventReplays(ReplaysRequest request)
    {
        var replays = context.Replays
            .Where(x => x.ReplayEvent != null)
            .AsNoTracking();

        if (request.DefaultFilter)
        {
            replays = replays.Where(x => x.DefaultFilter);
        }

        if (!String.IsNullOrEmpty(request.SearchPlayers))
        {
            replays = replays.Include(i => i.ReplayPlayers);
        }

        replays = replays.Where(x => x.GameTime >= request.StartTime);

        if (request.EndTime < DateTime.UtcNow.Date.AddDays(-2))
        {
            replays = replays.Where(x => x.GameTime < request.EndTime);
        }

        if (request.PlayerCount != 0)
        {
            replays = replays.Where(x => x.Playercount == request.PlayerCount);
        }

        if (!String.IsNullOrEmpty(request.Tournament))
        {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            replays = replays.Where(x => x.ReplayEvent.Event.Name.Equals(request.Tournament));
#pragma warning restore CS8602 // Dereference of a possibly null reference.
        }

        if (request.GameModes.Any())
        {
            replays = replays.Where(x => request.GameModes.Contains(x.GameMode));
        }

        if (request.ResultAdjusted)
        {
            replays = replays.Where(x => x.ResultCorrected);
        }

        if (request.ToonId == 0)
        {
            replays = SearchReplays(replays, request, withEvent: true);
        }
        else
        {
            replays = SearchToonIds(replays, request);
        }

        return replays;
    }
}
