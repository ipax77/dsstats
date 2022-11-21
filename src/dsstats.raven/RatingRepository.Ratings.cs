using dsstats.raven.Extensions;
using pax.dsstats.shared;
using Raven.Client.Documents;
using Raven.Client.Documents.Linq;
using Raven.Client.Documents.Session;

namespace dsstats.raven;

public partial class RatingRepository
{
    public async Task<PlayerRatingsResult> GetRatings(RatingsRequest request, CancellationToken token)
    {
        IQueryable<PlayerRatingBase> ratings;
        int count;

        if (request.Type == RatingType.Cmdr)
        {
            if (!ToonIdCmdrRatings.Any())
            {
                return await GetRatingsFromRaven(request, token);
            }
            ratings = ToonIdCmdrRatings.Values.AsQueryable();
            count = ToonIdCmdrRatings.Count;
        }
        else if (request.Type == RatingType.Std)
        {
            if (!ToonIdStdRatings.Any())
            {
                return await GetRatingsFromRaven(request, token);
            }
            ratings = ToonIdStdRatings.Values.AsQueryable();
            count = ToonIdStdRatings.Count;
        }
        else
        {
            throw new NotImplementedException();
        }

        foreach (var order in request.Orders)
        {
            if (order.Ascending)
            {
                ratings = ratings.AppendOrderBy(order.Property);
            }
            else
            {
                ratings = ratings.AppendOrderByDescending(order.Property);
            }
        }

        if (!String.IsNullOrEmpty(request.Search))
        {
            ratings = ratings.Where(x => request.Search.ToUpper().Contains(request.Search.ToUpper()));
        }

        return new PlayerRatingsResult()
        {
            Count = count,
            PlayerRatings = ratings.Skip(request.Skip).Take(request.Take).ToList()
        };
    }

    public async Task<PlayerRatingsResult> GetRatingsFromRaven(RatingsRequest request, CancellationToken token)
    {
        if (request.Type == RatingType.Cmdr)
        {
            return await GetCmdrRatingsFromRaven(request, token);
        }
        else if (request.Type == RatingType.Std)
        {
            return await GetStdRatingsFromRaven(request, token);
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    private async Task<PlayerRatingsResult> GetCmdrRatingsFromRaven(RatingsRequest request, CancellationToken token)
    {
        using var session = DocumentStoreHolder.Store.OpenAsyncSession();

        var query = GetQueriablePlayers(session, request);

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
            PlayerRatings = playerRatigns.Select(s => new PlayerRatingBase()
            {
                Games = s.Games,
                Main = s.Main,
                MainPercentage = s.MainPercentage,
                Mmr = s.Mmr,
                RegionId = s.RegionId,
                ToonId = s.ToonId,
                Name = s.Name,
                Wins = s.Wins,
                Mvp = s.Mvp,
            }).ToList()
        };
    }

    private static IRavenQueryable<PlayerRatingCmdr_ForTable.Result> SetOrder(IRavenQueryable<PlayerRatingCmdr_ForTable.Result> players, List<TableOrder> orders)
    {
        foreach (var order in orders)
        {
            if (order.Ascending)
            {
                players = players.RavenAppendOrderBy(order.Property);
            }
            else
            {
                players = players.RavenAppendOrderByDescending(order.Property);
            }
        }
        return players;
    }

    private static IRavenQueryable<PlayerRatingCmdr_ForTable.Result> FilterRatingPlayers(IRavenQueryable<PlayerRatingCmdr_ForTable.Result> players, string? searchString)
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

    private static IRavenQueryable<PlayerRatingCmdr_ForTable.Result> GetQueriablePlayers(IAsyncDocumentSession session, RatingsRequest request)
    {
        return session.Query<PlayerRatingCmdr_ForTable.Result, PlayerRatingCmdr_ForTable>()
            .Where(x => x.Games >= 20);
    }

    private async Task<PlayerRatingsResult> GetStdRatingsFromRaven(RatingsRequest request, CancellationToken token)
    {
        using var session = DocumentStoreHolder.Store.OpenAsyncSession();

        var query = GetStdQueriablePlayers(session, request);

        query = FilterStdRatingPlayers(query, request.Search);
        query = SetStdOrder(query, request.Orders);

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
            PlayerRatings = playerRatigns.Select(s => new PlayerRatingBase()
            {
                Games = s.Games,
                Main = s.Main,
                MainPercentage = s.MainPercentage,
                Mmr = s.Mmr,
                RegionId = s.RegionId,
                ToonId = s.ToonId,
                Name = s.Name,
                Wins = s.Wins,
                Mvp = s.Mvp,
            }).ToList()
        };
    }

    private static IRavenQueryable<PlayerRatingStd_ForTable.Result> SetStdOrder(IRavenQueryable<PlayerRatingStd_ForTable.Result> players, List<TableOrder> orders)
    {
        foreach (var order in orders)
        {
            if (order.Ascending)
            {
                players = players.RavenAppendOrderBy(order.Property);
            }
            else
            {
                players = players.RavenAppendOrderByDescending(order.Property);
            }
        }
        return players;
    }

    private static IRavenQueryable<PlayerRatingStd_ForTable.Result> FilterStdRatingPlayers(IRavenQueryable<PlayerRatingStd_ForTable.Result> players, string? searchString)
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

    private static IRavenQueryable<PlayerRatingStd_ForTable.Result> GetStdQueriablePlayers(IAsyncDocumentSession session, RatingsRequest request)
    {
        return session.Query<PlayerRatingStd_ForTable.Result, PlayerRatingStd_ForTable>()
            .Where(x => x.Games >= 20);
    }
}
