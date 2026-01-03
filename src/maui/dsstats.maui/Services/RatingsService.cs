using dsstats.shared;
using dsstats.shared.Interfaces;

namespace dsstats.maui.Services;

public class RatingsService : IRatingService
{
    public Task ContinueFindSc2ArcadeMatches(DateTime? lastCheckTime = null)
    {
        return Task.CompletedTask;
    }

    public Task ContinueRatings()
    {
        return Task.CompletedTask;
    }

    public Task CreateRatings()
    {
        return Task.CompletedTask;
    }

    public Task FindSc2ArcadeMatches()
    {
        return Task.CompletedTask;
    }

    public Task MatchNewDsstatsReplays(DateTime? dsstatsImportedAfter = null)
    {
        return Task.CompletedTask;
    }

    public Task MatchWithNewArcadeReplays(DateTime? arcadeImportedAfter = null)
    {
        return Task.CompletedTask;
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
