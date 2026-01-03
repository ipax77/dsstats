namespace dsstats.shared.Interfaces;

public interface IReplayRepository
{
    Task<ReplayDetails?> GetReplayDetails(string replayHash);
    Task<ReplayRatingDto?> GetReplayRating(string replayHash);
    Task SaveReplayRatingAll(string replayHash, ReplayRatingDto rating);
    Task<ReplayDetails?> GetLatestReplay();
    Task<ReplayDetails?> GetNextReplay(bool after, string replayHash);
    Task<List<ReplayListDto>> GetReplays(ReplaysRequest request, CancellationToken token = default);
    Task<int> GetReplaysCount(ReplaysRequest request, CancellationToken token = default);
    Task<ReplayDetails?> GetArcadeReplayDetails(string replayHash);
    Task<List<ReplayListDto>> GetArcadeReplays(ArcadeReplaysRequest request, CancellationToken token = default);
    Task<int> GetArcadeReplaysCount(ArcadeReplaysRequest request, CancellationToken token = default);
}
