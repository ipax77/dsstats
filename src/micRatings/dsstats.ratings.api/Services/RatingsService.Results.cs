using pax.dsstats.shared;

namespace dsstats.ratings.api.Services;

public partial class RatingsService
{
    public List<RatingsReport> GetResults()
    {
        return ratingsResults.ToList();
    }
}
