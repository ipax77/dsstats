namespace dsstats.shared.Interfaces;

public interface IRatingService
{
    Task CreateRatings();
    Task ContinueRatings();
    Task PreRatings(List<int> replayIds);
    Task PreRatings(List<ReplayCalcDto> replays);
    Task ContinueFindSc2ArcadeMatches(DateTime? lastCheckTime = null);
    Task FindSc2ArcadeMatches();
    Task MatchWithNewArcadeReplays(DateTime? arcadeImportedAfter = null);
    Task MatchNewDsstatsReplays(DateTime? dsstatsImportedAfter = null);
}