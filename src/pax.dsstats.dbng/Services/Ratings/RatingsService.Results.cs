using pax.dsstats.shared;

namespace pax.dsstats.dbng.Services.Ratings;

public partial class RatingsService
{
    public List<RatingsReport> GetResults()
    {
        return ratingsResults.ToList();
    }
}
