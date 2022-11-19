
using dsstats.raven.Extensions;
using pax.dsstats.shared;
using Raven.Client.Documents;
using Raven.Client.Documents.BulkInsert;
using Raven.Client.Documents.Linq;
using Raven.Client.Documents.Operations;
using Raven.Client.Documents.Session;

namespace dsstats.raven;

#pragma warning disable CA1822 // insterface and static are not best friends
public class RatingRepository : IRatingRepository
{
    public async Task DeleteRatings()
    {
        var ratingOperation = await DocumentStoreHolder.Store
            .Operations
            .SendAsync(new DeleteByQueryOperation<PlayerRating, PlayerRating_ByPlayerId>(x => x.PlayerId > 0));

        var changeOperation = await DocumentStoreHolder.Store
            .Operations
            .SendAsync(new DeleteByQueryOperation<ReplayPlayerMmrChange, ReplayPlayerMmrChange_ByReplayPlayerId>(x => x.ReplayPlayerId > 0));
    }

    public async Task<UpdateResult> UpdateReplayPlayerMmrChanges(List<ReplayPlayerMmrChange> replayPlayerMmrChanges)
    {
        var chunks = replayPlayerMmrChanges.OrderBy(o => o.ReplayPlayerId).Chunk(10000);
        UpdateResult updateResult = new() { Total = replayPlayerMmrChanges.Count };

        foreach (var chunk in chunks)
        {
            int startId = chunk.First().ReplayPlayerId;
            int endId = chunk.Last().ReplayPlayerId;

            using var session = DocumentStoreHolder.Store.OpenAsyncSession();
            using BulkInsertOperation bulkInsert = DocumentStoreHolder.Store.BulkInsert();

            var changes = (await session.Query<ReplayPlayerMmrChange, ReplayPlayerMmrChange_ByReplayPlayerId>()
                .Where(x => x.ReplayPlayerId >= startId && x.ReplayPlayerId <= endId)
                .ToListAsync()).ToDictionary(k => k.ReplayPlayerId, v => v);

            for (int i = 0; i < chunk.Length; i++)
            {
                var newChange = chunk[i];
                if (changes.ContainsKey(newChange.ReplayPlayerId))
                {
                    changes[newChange.ReplayPlayerId].MmrChange = newChange.MmrChange;
                    updateResult.Update++;
                }
                else
                {
                    await bulkInsert.StoreAsync(newChange);
                    updateResult.New++;
                }
            }
            await session.SaveChangesAsync();
        }
        return updateResult;
    }

    public async Task<UpdateResult> UpdatePlayerRatings(List<PlayerRating> playerRatings)
    {
        var chunks = playerRatings.OrderBy(o => o.PlayerId).Chunk(10000);


        UpdateResult updateResult = new() { Total = playerRatings.Count };
        foreach (var chunk in chunks)
        {
            using var session = DocumentStoreHolder.Store.OpenAsyncSession();
            using BulkInsertOperation bulkInsert = DocumentStoreHolder.Store.BulkInsert();

            int startId = chunk.First().PlayerId;
            int endId = chunk.Last().PlayerId;

            var ratings = (await session.Query<PlayerRating, PlayerRating_ByPlayerId>()
                .Where(x => x.PlayerId >= startId && x.PlayerId <= endId)
                .ToListAsync()).ToDictionary(k => k.PlayerId, v => v);

            for (int i = 0; i < chunk.Length; i++)
            {
                var newRating = chunk[i];
                if (ratings.ContainsKey(newRating.PlayerId))
                {
                    var dbRating = ratings[newRating.PlayerId];
                    dbRating.Mmr = newRating.Mmr;
                    // ...
                    updateResult.Update++;
                }
                else
                {
                    await bulkInsert.StoreAsync(newRating);
                    updateResult.New++;
                }
            }
            await session.SaveChangesAsync();
        }
        return updateResult;
    }

    public async Task<string?> GetPlayerRatings(int toonId, CancellationToken token)
    {
        // todo: mmrId
        using var session = DocumentStoreHolder.Store.OpenAsyncSession();

        return await session.Query<PlayerRating, PlayerRating_ByToonId>()
            .Where(x => x.ToonId == toonId)
            .Select(s => s.MmrOverTime)
            .FirstOrDefaultAsync(token);
    }

    public async Task<PlayerRating?> GetPlayerRating(int toonId, CancellationToken token)
    {
        // todo: mmrId
        using var session = DocumentStoreHolder.Store.OpenAsyncSession();

        return await session.Query<PlayerRating, PlayerRating_ByToonId>()
            .Where(x => x.ToonId == toonId)
            .FirstOrDefaultAsync(token);
    }

    public async Task<List<ReplayPlayerMmrChange>> GetReplayPlayerMmrChanges(List<int> replayPlayerIds, CancellationToken token)
    {
        using var session = DocumentStoreHolder.Store.OpenAsyncSession();

        return await session.Query<ReplayPlayerMmrChange, ReplayPlayerMmrChange_ByReplayPlayerId>()
            // .Where(x => replayPlayerIds.Contains(x.ReplayPlayerId))
            .Where(x => x.ReplayPlayerId.In(replayPlayerIds))
            .ToListAsync(token);
    }

    public async Task<PlayerRatingsResult> GetRatings(RatingsRequest request, CancellationToken token)
    {
        using var session = DocumentStoreHolder.Store.OpenAsyncSession();

        var query = GetQueriablePlayers(session);
        query = FilterRatingPlayers(query, request.Search);
        query = SetOrder(query, request.Orders);

        //var bab = query.ToAsyncDocumentQuery();

        var playerRatigns = await query
            //.OrderByDescending(o => o.Mmr)
            .Statistics(out QueryStatistics stats)
            .Skip(request.Skip)
            .Take(request.Take)
            .ToListAsync(token);

        return new()
        {
            Count = stats.TotalResults,
            PlayerRatings = playerRatigns
        };
    }

    private static IRavenQueryable<PlayerRating> SetOrder(IRavenQueryable<PlayerRating> players, List<TableOrder> orders)
    {
        //foreach (var order in orders)
        //{
        //    if (order.Ascending)
        //    {
        //        players = players.AppendOrderBy(order.Property);
        //    }
        //    else
        //    {
        //        players = players.AppendOrderByDescending(order.Property);
        //    }
        //}

        //var order = orders.LastOrDefault();
        //if (order != null)
        //{
        //    var prop = typeof(PlayerRating).GetProperty(order.Property);
        //    if (prop != null)
        //    {
        //        if (order.Ascending)
        //        {
        //            players = players.OrderBy(prop);
        //        }
        //        else
        //        {
        //            players = players.OrderByDescending(prop.Name);
        //        }
        //    }
        //}

        var order = orders.LastOrDefault();
        if (order != null)
        {
            if (order.Property == nameof(PlayerRating.Mmr))
            {
                if (order.Ascending)
                {
                    players = players.OrderBy(o => o.Mmr);
                }
                else
                {
                    players = players.OrderByDescending(o => o.Mmr);
                }
            }
            else if (order.Property == nameof(PlayerRating.Name))
            {
                if (order.Ascending)
                {
                    players = players.OrderBy(o => o.Name);
                }
                else
                {
                    players = players.OrderByDescending(o => o.Name);
                }
            }
            else if (order.Property == nameof(PlayerRating.Games))
            {
                if (order.Ascending)
                {
                    players = players.OrderBy(o => o.Games);
                }
                else
                {
                    players = players.OrderByDescending(o => o.Games);
                }
            }
            else if (order.Property == nameof(PlayerRating.Mvp))
            {
                if (order.Ascending)
                {
                    players = players.OrderBy(o => o.Mvp);
                }
                else
                {
                    players = players.OrderByDescending(o => o.Mvp);
                }
            }
            else if (order.Property == nameof(PlayerRating.Mvprate))
            {
                if (order.Ascending)
                {
                    players = players.OrderBy(o => o.Mvprate);
                }
                else
                {
                    players = players.OrderByDescending(o => o.Mvprate);
                }
            }
            else if (order.Property == nameof(PlayerRating.Mvprate))
            {
                if (order.Ascending)
                {
                    players = players.OrderBy(o => o.Mvprate);
                }
                else
                {
                    players = players.OrderByDescending(o => o.Mvprate);
                }
            }
            else if (order.Property == nameof(PlayerRating.Winrate))
            {
                if (order.Ascending)
                {
                    players = players.OrderBy(o => o.Winrate);
                }
                else
                {
                    players = players.OrderByDescending(o => o.Winrate);
                }
            }
            else if (order.Property == nameof(PlayerRating.RegionId))
            {
                if (order.Ascending)
                {
                    players = players.OrderBy(o => o.RegionId);
                }
                else
                {
                    players = players.OrderByDescending(o => o.RegionId);
                }
            }
        }

        return players;
    }

    private static IRavenQueryable<PlayerRating> FilterRatingPlayers(IRavenQueryable<PlayerRating> players, string? searchString)
    {
        if (string.IsNullOrEmpty(searchString))
        {
            return players;
        }

        if (int.TryParse(searchString, out int toonId))
        {
            return players.Where(x => x.ToonId == toonId);
        }

        // return players.Where(x => x.Name.ToUpper().Contains(searchString.ToUpper()));

        return players.Search(s => s.Name, searchString);

    }

    private static IRavenQueryable<PlayerRating> GetQueriablePlayers(IAsyncDocumentSession session)
    {
        return session.Query<PlayerRating>()
            .Where(x => x.Games >= 20);
    }

    public async Task<List<MmrDevDto>> GetRatingsDeviation()
    {
        using var session = DocumentStoreHolder.Store.OpenAsyncSession();

        return await session.Query<PlayerRating>()
            .GroupBy(g => Math.Round(g.Mmr, 0))
            .Select(s => new MmrDevDto
            {
                Count = s.Count(),
                Mmr = s.Average(a => Math.Round(a.Mmr, 0))
            })
            .OrderBy(o => o.Mmr)
            .ToListAsync();
    }
}
#pragma warning restore CA1822
