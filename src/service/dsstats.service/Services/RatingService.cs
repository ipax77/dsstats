using dsstats.shared;
using dsstats.shared.Interfaces;

namespace dsstats.service.Services;

internal sealed class RatingService : IRatingService
{
    public Task ContinueFindSc2ArcadeMatches(DateTime? lastCheckTime = null)
    {
        throw new NotImplementedException();
    }

    public Task ContinueRatings()
    {
        throw new NotImplementedException();
    }

    public Task CreateRatings()
    {
        throw new NotImplementedException();
    }

    public Task FindSc2ArcadeMatches()
    {
        throw new NotImplementedException();
    }

    public Task MatchNewDsstatsReplays(DateTime? dsstatsImportedAfter = null)
    {
        throw new NotImplementedException();
    }

    public Task MatchWithNewArcadeReplays(DateTime? arcadeImportedAfter = null)
    {
        throw new NotImplementedException();
    }

    public Task PreRatings(List<int> replayIds)
    {
        return Task.CompletedTask;
    }

    public Task PreRatings(List<ReplayCalcDto> replays)
    {
        return Task.CompletedTask;
    }
}
