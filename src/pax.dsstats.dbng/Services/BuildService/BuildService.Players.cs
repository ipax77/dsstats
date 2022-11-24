using pax.dsstats.shared;

namespace pax.dsstats.dbng.Services;
public partial class BuildService
{
    public List<RequestNames> GetTopPlayers(bool std = false, int minGames = 100)
    {
        return ratingRepository.GetTopPlayers(std ? shared.Raven.RatingType.Std : shared.Raven.RatingType.Cmdr, minGames);   
    }
}
