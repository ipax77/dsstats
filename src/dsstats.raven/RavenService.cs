
using dsstats.raven.Extensions;
using pax.dsstats.shared;
using Raven.Client.Documents;
using Raven.Client.Documents.BulkInsert;
using Raven.Client.Documents.Operations;
using Raven.Client.Documents.Session;

namespace dsstats.raven;

public static class RavenService
{
    public static async Task DeleteRatings()
    {
        var ratingOperation = await DocumentStoreHolder.Store
            .Operations
            .SendAsync(new DeleteByQueryOperation<PlayerRating, PlayerRating_ByPlayerId>(x => x.PlayerId > 0));

        var changeOperation = await DocumentStoreHolder.Store
            .Operations
            .SendAsync(new DeleteByQueryOperation<ReplayPlayerMmrChange, ReplayPlayerMmrChange_ByReplayPlayerId>(x => x.ReplayPlayerId > 0));
    }

    public static async Task BulkInsert(List<PlayerRating> playerRatings)
    {
        using BulkInsertOperation bulkInsert = DocumentStoreHolder.Store.BulkInsert();

        for (int i = 0; i < playerRatings.Count; i++)
        {
            await bulkInsert.StoreAsync(playerRatings[i]);
        }
    }

    public static async Task BulkInsert(List<ReplayPlayerMmrChange> replayPlayerMmrChanges)
    {
        using BulkInsertOperation bulkInsert = DocumentStoreHolder.Store.BulkInsert();

        for (int i = 0; i < replayPlayerMmrChanges.Count; i++)
        {
            await bulkInsert.StoreAsync(replayPlayerMmrChanges[i]);
        }
    }

    public static async Task<UpdateResult> UpdateReplayPlayermmrChanges(List<ReplayPlayerMmrChange> replayPlayerMmrChanges)
    {
        var chunks = replayPlayerMmrChanges.OrderBy(o => o.ReplayPlayerId).Chunk(10000);
        UpdateResult updateResult = new() { Total = replayPlayerMmrChanges.Count };

        foreach(var chunk in chunks)
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

    public static async Task<UpdateResult> UpdatePlayerRatings(List<PlayerRating> playerRatings)
    {
        var chunks = playerRatings.OrderBy(o => o.PlayerId).Chunk(10000);

        
        UpdateResult updateResult = new() { Total = playerRatings.Count };
        List<PlayerRating> newRatings = new();
        foreach (var chunk in chunks)
        {
            using var session = DocumentStoreHolder.Store.OpenAsyncSession();
            
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
                    newRatings.Add(newRating);
                    updateResult.New++;
                }
            }
            await session.SaveChangesAsync();
        }

        if (newRatings.Any())
        {
            await BulkInsert(newRatings);
        }
        return updateResult;
    }

    public static async Task<List<PlayerRating>> GetPlayerRatings(RatingsRequest request)
    {
        using var session = DocumentStoreHolder.Store.OpenAsyncSession();

        var query = GetQueriablePlayers(session);
        query = FilterRatingPlayers(query, request.Search);
        query = SetOrder(query, request.Orders);

        return await query
            .Skip(request.Skip)
            .Take(request.Take)
            .ToListAsync();
    }

    private static IQueryable<PlayerRating> SetOrder(IQueryable<PlayerRating> players, List<TableOrder> orders)
    {
        foreach (var order in orders)
        {
            if (order.Property == "Winrate")
            {
                if (order.Ascending)
                {
                    players = players.OrderBy(o => o.Games > 0 ? o.Wins * 100.0 / o.Games : o.Wins);
                }
                else
                {
                    players = players.OrderByDescending(o => o.Games > 0 ? o.Wins * 100.0 / o.Games : o.Wins);
                }
            }
            else if (order.Property == "Mvprate")
            {
                if (order.Ascending)
                {
                    players = players.OrderBy(o => o.Games > 0 ? o.Mvp * 100.0 / o.Games : o.Mvp);
                }
                else
                {
                    players = players.OrderByDescending(o => o.Games > 0 ? o.Mvp * 100.0 / o.Games : o.Mvp);
                }
            }
            else if (order.Property == "Teamgames")
            {
                if (order.Ascending)
                {
                    players = players.OrderBy(o => o.Games > 0 ? o.TeamGames * 100.0 / o.Games : o.Games);
                }
                else
                {
                    players = players.OrderByDescending(o => o.Games > 0 ? o.TeamGames * 100.0 / o.Games : o.Games);
                }
            }
            else
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
        }
        return players;
    }

    private static IQueryable<PlayerRating> FilterRatingPlayers(IQueryable<PlayerRating> players, string? searchString)
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

    private static IQueryable<PlayerRating> GetQueriablePlayers(IAsyncDocumentSession session)
    {
        return session.Query<PlayerRating>()
            .OrderBy(o => o.PlayerId)
            .Where(x => x.Games >= 20)
            .AsQueryable();
    }
}

public record UpdateResult
{
    public int Total { get; set; }
    public int Update { get; set; }
    public int New { get; set; }
}