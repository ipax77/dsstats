using pax.dsstats.shared;

namespace pax.dsstats.web.Server.Services.Ratings;

public partial class RatingsService
{
    public List<RatingsReport> GetResults()
    {
        return ratingsResults.ToList();
    }
}
