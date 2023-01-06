using pax.dsstats.shared;

namespace pax.dsstats.dbng.Services;
public partial class BuildService
{
    public async Task<List<RequestNames>> GetTopPlayers(bool std = false, int minGames = 100)
    {
        return await ratingRepository.GetTopPlayers(std ? shared.RatingType.Std : shared.RatingType.Cmdr, minGames);   
    }
}
