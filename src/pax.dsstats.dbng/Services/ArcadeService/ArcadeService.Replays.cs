using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using pax.dsstats.dbng.Extensions;
using pax.dsstats.shared;
using pax.dsstats.shared.Arcade;

namespace pax.dsstats.dbng.Services;

public partial class ArcadeService
{
    public async Task<ArcadeReplayDto?> GetArcadeReplay(int id, CancellationToken token = default)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        return await context.ArcadeReplays
            .Where(x => x.Id == id)
            .ProjectTo<ArcadeReplayDto>(mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(token);
    }

    public async Task<int> GetReplayCount(ArcadeReplaysRequest request, CancellationToken token)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var replays = GetArcadeReplaysQueriable(request, context);
        return await replays.CountAsync(token);
    }

    public async Task<List<ArcadeReplayListDto>> GetArcadeReplays(ArcadeReplaysRequest request, CancellationToken token)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var replays = GetArcadeReplaysQueriable(request, context);

        replays = SortReplays(request, replays);

        return await replays
            .Skip(request.Skip)
            .Take(request.Take)
            .ProjectTo<ArcadeReplayListDto>(mapper.ConfigurationProvider)
            .ToListAsync();
    }

    private IQueryable<ArcadeReplay> SortReplays(ArcadeReplaysRequest request, IQueryable<ArcadeReplay> replays)
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

    private IQueryable<ArcadeReplay> GetArcadeReplaysQueriable(ArcadeReplaysRequest request, ReplayContext context)
    {
        IQueryable<ArcadeReplay> replays;

        var names = request.Search == null ? new()
            : request.Search.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .Distinct()
            .ToList();

        if (names.Count == 0)
        {
            replays = context.ArcadeReplays
                .Where(x => x.CreatedAt > new DateTime(2021, 2, 1))
                .AsNoTracking();
        }
        else if (names.Count == 1)
        {
            var name = names[0];
            replays = from rp in context.ArcadeReplayPlayers
                      where rp.ArcadeReplay.CreatedAt > new DateTime(2021, 2, 1)
                        && rp.Name == name
                      select rp.ArcadeReplay;
        }
        else
        {
            var name = names[0];
            replays = from rp in context.ArcadeReplayPlayers
                      where rp.ArcadeReplay.CreatedAt > new DateTime(2021, 2, 1)
                        && rp.Name == name
                      select rp.ArcadeReplay;
            for (int i = 1; i < names.Count; i++)
            {
                var iname = names[i];
                replays = replays.Where(x => x.ArcadeReplayPlayers.Any(a => a.Name == iname));
            }
        }

        if (request.GameMode != GameMode.None)
        {
            replays = replays.Where(x => x.GameMode == request.GameMode);
        }

        if (request.RegionId > 0)
        {
            replays = replays.Where(x => x.RegionId == request.RegionId);
        }
        return replays;
    }
}



