using pax.dsstats.shared;

namespace pax.dsstats.dbng.Services;
public partial class BuildService
{
    public List<RequestNames> GetTopPlayers(int minGames = 100)
    {
        var topPlayersCmdr = mmrService.ToonIdRatings.Values
            .Where(x => x.CmdrRatingStats.Games >= minGames)
            .OrderByDescending(o => o.CmdrRatingStats.Wins * 100.0 / o.CmdrRatingStats.Games)
            .Take(5)
            .ToList();

        var topPlayersStd = mmrService.ToonIdRatings.Values
            .Where(x => x.StdRatingStats.Games >= minGames)
            .OrderByDescending(o => o.StdRatingStats.Wins * 100.0 / o.StdRatingStats.Games)
            .Take(5)
            .ToList();

        List<RequestNames> topNames = new() {
            new() { Name = "PAX", ToonId = 226401 },
            new() { Name = "PAX", ToonId = 10188255 },
            new() { Name = "Feralan", ToonId = 8497675 },
            new() { Name = "Feralan", ToonId = 1488340 }
        };

        var topPlayers = topPlayersCmdr.Union(topPlayersStd).Select(s => new RequestNames() { Name = s.Name, ToonId = s.ToonId });
        return topNames.Union(topPlayers).Distinct().ToList();
    }
}
