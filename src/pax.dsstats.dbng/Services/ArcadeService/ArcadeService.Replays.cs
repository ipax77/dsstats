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
            .Where(x => x.ArcadeReplayId == id)
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
        if (request.Skip < 0 || request.Take < 0)
        {
            return new();
        }

        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var replays = GetArcadeReplaysQueriable(request, context);

        replays = SortReplays(request, replays);

        if (request.PlayerId == 0)
        {
            return await replays
                .Skip(request.Skip)
                .Take(request.Take)
                .ProjectTo<ArcadeReplayListDto>(mapper.ConfigurationProvider)
                .ToListAsync(token);
        }
        else
        {
            return await GetRatingArcadeReplays(request, replays, token);
        }
    }

    private async Task<List<ArcadeReplayListDto>> GetRatingArcadeReplays(ArcadeReplaysRequest request,
                                                                         IQueryable<ArcadeReplay> replays,
                                                                         CancellationToken token)
    {
            var pageReplays = await replays
                .Skip(request.Skip)
                .Take(request.Take)
                .ProjectTo<ArcadeReplayListRatingDto>(mapper.ConfigurationProvider)
                .ToListAsync(token);

            for (int i = 0; i < pageReplays.Count; i++)
            {
                var replay = pageReplays[i];
                if (replay.ArcadeReplayRating == null)
                {
                    continue;
                }
                var pl = replay.ArcadeReplayPlayers
                    .FirstOrDefault(f => f.ArcadePlayer.ArcadePlayerId == request.PlayerId);

                if (pl == null)
                {
                    continue;
                }

                var rating = replay.ArcadeReplayRating.ArcadeReplayPlayerRatings
                    .FirstOrDefault(f => f.GamePos == pl.SlotNumber);

                if (rating == null)
                {
                    continue;
                }

                replay.MmrChange = rating.RatingChange;
            }
            pageReplays.ForEach(f =>
            {
                f.ArcadeReplayPlayers.Clear();
                f.ArcadeReplayRating = null;
            });
            return pageReplays.Cast<ArcadeReplayListDto>().ToList();
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

        if (request.PlayerId == 0)
        {
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
        }
        else
        {
            if (request.PlayerIdWith != 0)
            {
                replays = from rp in context.ArcadeReplayPlayers
                          from rp1 in context.ArcadeReplayPlayers
                          where rp.ArcadeReplay.CreatedAt > new DateTime(2021, 2, 1)
                            && rp.ArcadePlayer.ArcadePlayerId == request.PlayerId
                            && rp1.ArcadeReplayId == rp.ArcadeReplayId
                            && rp1.Team == rp.Team
                            && rp1.ArcadePlayer.ArcadePlayerId == request.PlayerIdWith
                          select rp.ArcadeReplay;
            }
            else if (request.PlayerIdVs != 0)
            {
                replays = from rp in context.ArcadeReplayPlayers
                          from rp1 in context.ArcadeReplayPlayers
                          where rp.ArcadeReplay.CreatedAt > new DateTime(2021, 2, 1)
                            && rp.ArcadePlayer.ArcadePlayerId == request.PlayerId
                            && rp1.ArcadeReplayId == rp.ArcadeReplayId
                            && rp1.Team != rp.Team
                            && rp1.ArcadePlayer.ArcadePlayerId == request.PlayerIdVs
                          select rp.ArcadeReplay;
            }
            else
            {

                replays = from rp in context.ArcadeReplayPlayers
                          where rp.ArcadeReplay.CreatedAt > new DateTime(2021, 2, 1)
                            && rp.ArcadePlayer.ArcadePlayerId == request.PlayerId
                          select rp.ArcadeReplay;
            }

            //if (!String.IsNullOrEmpty(request.Search))
            //{
            //    var names = request.Search.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            //        .Select(x => x.Trim())
            //        .Distinct()
            //        .ToList();

            //    foreach (var name in names)
            //    {
            //        replays = replays.Where(x => x.ArcadeReplayPlayers.Any(a => a.Name == name));
            //    }
            //}
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



