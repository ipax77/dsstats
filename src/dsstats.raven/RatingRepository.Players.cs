using Newtonsoft.Json.Linq;
using pax.dsstats.shared;
using Raven.Client.Documents;

namespace dsstats.raven;

public partial class RatingRepository : IRatingRepository
{
    public List<RequestNames> GetTopPlayersStd(int minGames)
    {
        var topPlayersStd = ToonIdStdRatings.Values
            .Where(x => x.Games >= minGames)
            .OrderByDescending(o => o.Wins * 100.0 / o.Games)
            .Take(5)
            .ToList();

        return topPlayersStd.Select(s => new RequestNames()
        {
            Name = s.Name,
            ToonId = s.ToonId
        }).ToList();
    }

    public List<RequestNames> GetTopPlayersCmdr(int minGames)
    {
        var topPlayersCmdr = ToonIdCmdrRatings.Values
            .Where(x => x.Games >= minGames)
            .OrderByDescending(o => o.Wins * 100.0 / o.Games)
            .Take(5)
            .ToList();

        return topPlayersCmdr.Select(s => new RequestNames()
        {
            Name = s.Name,
            ToonId = s.ToonId
        }).ToList();
    }

    public async Task<string?> GetToonIdName(int toonId)
    {
        if (ToonIdCmdrRatings.ContainsKey(toonId))
        {
            return ToonIdCmdrRatings[toonId].Name;
        }

        if (ToonIdStdRatings.ContainsKey(toonId))
        {
            return ToonIdStdRatings[toonId].Name;
        }

        using var session = DocumentStoreHolder.Store.OpenAsyncSession();

        return await session.Query<PlayerRatingCmdr, PlayerRatingCmdr_ByToonId>()
            .Where(x => x.ToonId == toonId)
            .Select(s => s.Name)
            .FirstOrDefaultAsync();
    }
}
