
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