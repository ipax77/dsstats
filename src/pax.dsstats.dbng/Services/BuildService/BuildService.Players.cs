using pax.dsstats.shared;

namespace pax.dsstats.dbng.Services;
public partial class BuildService
{
    public  List<RequestNames> GetTopPlayersStd(int minGames)
    {
        return ratingRepository.GetTopPlayersStd(minGames);
    }

    public List<RequestNames> GetTopPlayersCmdr(int minGames)
    {
        return ratingRepository.GetTopPlayersCmdr(minGames);
    }
}
