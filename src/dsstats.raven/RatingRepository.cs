
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

    public async Task<UpdateResult> UpdatePlayerRatings<T>(List<PlayerRatingBase> playerRatings) where T : PlayerRatingBase, new()
    {
        var chunks = playerRatings.OrderBy(o => o.PlayerId).Chunk(10000);

        UpdateResult updateResult = new() { Total = playerRatings.Count };
        foreach (var chunk in chunks)
        {
            using var session = DocumentStoreHolder.Store.OpenAsyncSession();
            using BulkInsertOperation bulkInsert = DocumentStoreHolder.Store.BulkInsert();

            int startId = chunk.First().PlayerId;
            int endId = chunk.Last().PlayerId;

            Dictionary<int, T> playerIdRatings;

            if (typeof(T) == typeof(PlayerRatingCmdr))
            {
                playerIdRatings = (await session.Query<PlayerRatingCmdr, PlayerRatingCmdr_ByPlayerId>()
                    .Where(x => x.PlayerId >= startId && x.PlayerId <= endId)
                    .ToListAsync()).ToDictionary(k => k.PlayerId, v => v as T ?? throw new ArgumentException(nameof(T)));

            }
            else if (typeof(T) == typeof(PlayerRatingStd))
            {
                playerIdRatings = (await session.Query<PlayerRatingStd, PlayerRatingStd_ByPlayerId>()
                    .Where(x => x.PlayerId >= startId && x.PlayerId <= endId)
                    .ToListAsync()).ToDictionary(k => k.PlayerId, v => v as T ?? throw new ArgumentException(nameof(T)));
            }
            else
            {
                throw new NotImplementedException();
            }

            for (int i = 0; i < chunk.Length; i++)
            {
                T? newRating;

                if (typeof(T) == typeof(PlayerRatingCmdr))
                {
                    newRating = new PlayerRatingCmdr(chunk[i]) as T;
                }
                else if (typeof(T) == typeof(PlayerRatingStd))
                {
                    newRating = new PlayerRatingStd(chunk[i]) as T;
                }
                else
                {
                    throw new NotImplementedException();
                }

                if (newRating == null)
                {
                    continue;
                }

                if (playerIdRatings.ContainsKey(newRating.PlayerId))
                {
                    var dbRating = playerIdRatings[newRating.PlayerId];
                    dbRating.Games = newRating.Games;
                    dbRating.Wins = newRating.Wins;
                    dbRating.Mvp = newRating.Mvp;
                    dbRating.Mmr = newRating.Mmr;
                    dbRating.MmrOverTime = newRating.MmrOverTime;
                    dbRating.Consistency = newRating.Consistency;
                    dbRating.Uncertainty = newRating.Uncertainty;
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

        return (await session.Query<PlayerRatingCmdr, PlayerRatingCmdr_ByToonId>()
            .Where(x => x.ToonId == toonId)
            .Select(s => s.MmrOverTime)
            .FirstOrDefaultAsync(token))
            .ToString(); // todo
    }

    public async Task<PlayerRatingBase?> GetPlayerRating(int toonId, CancellationToken token)
    {
        // todo: mmrId
        using var session = DocumentStoreHolder.Store.OpenAsyncSession();

        return await session.Query<PlayerRatingCmdr, PlayerRatingCmdr_ByToonId>()
            .Where(x => x.ToonId == toonId)
            .FirstOrDefaultAsync(token);
    }

    public async Task<List<ReplayPlayerMmrChange>> GetReplayPlayerMmrChanges(List<int> replayPlayerIds, CancellationToken token)
    {
        using var session = DocumentStoreHolder.Store.OpenAsyncSession();

        return await session.Query<ReplayPlayerMmrChange, ReplayPlayerMmrChange_ByReplayPlayerId>()
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
            PlayerRatings = playerRatigns.Cast<PlayerRatingBase>().ToList()
        };
    }

    private static IRavenQueryable<PlayerRatingCmdr> SetOrder(IRavenQueryable<PlayerRatingCmdr> players, List<TableOrder> orders)
    {
        foreach (var order in orders)
        {
            if (order.Ascending)
            {
                players = players.AppendOrderBy(order.Property);
            }
            else
            {
                players = players.AppendOrderByDescending(order.Property);
            }
        }
        return players;
    }

    private static IRavenQueryable<PlayerRatingCmdr> FilterRatingPlayers(IRavenQueryable<PlayerRatingCmdr> players, string? searchString)
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

    private static IRavenQueryable<PlayerRatingCmdr> GetQueriablePlayers(IAsyncDocumentSession session)
    {
        return session.Query<PlayerRatingCmdr, PlayerRatingCmdr_ByGamesAndMainAndMainPercentageAndMmrAndMvprateAndRegionIdAndWinrateAndSearchName>()
            .Where(x => x.Games >= 20);
    }

    public async Task<List<MmrDevDto>> GetRatingsDeviation()
    {
        using var session = DocumentStoreHolder.Store.OpenAsyncSession();

        return await session.Query<PlayerRatingCmdr>()
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
