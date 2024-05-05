namespace dsstats.shared.Interfaces;

public interface ITourneysService
{
    Task<List<TourneysReplayListDto>> GetReplays(TourneysReplaysRequest request, CancellationToken token);
    Task<int> GetReplaysCount(TourneysReplaysRequest request, CancellationToken token = default);
    Task<List<TourneyDto>> GetTourneys();
    Task<TourneysStatsResponse> GetTourneyStats(TourneysStatsRequest statsRequest, CancellationToken token);
    Task<(string, string)?> DownloadReplay(string replayHash);
    Task SeedTourneys();
    Task<List<GroupStateDto>> GetGroupStates();
}