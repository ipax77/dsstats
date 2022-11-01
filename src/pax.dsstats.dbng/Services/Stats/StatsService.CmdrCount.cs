using pax.dsstats.shared;

namespace pax.dsstats.dbng.Services;

public partial class StatsService
{
    public async Task<StatsResponse> GetCmdrsCount(StatsRequest request)
    {
        return new();
    }
}
