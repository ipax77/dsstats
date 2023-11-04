
namespace dsstats.shared.Interfaces;

public interface IReplaysService
{
    Task<ReplaysResponse> GetReplays(ReplaysRequest request, CancellationToken token = default);
    Task<int> GetReplaysCount(ReplaysRequest request, CancellationToken token = default);
    Task<ReplayDto?> GetReplay(string replayHash, bool dry = false, CancellationToken token = default);
    Task<ReplayRatingDto?> GetReplayRating(string replayHash, bool comboRating);
    Task<ArcadeReplayDto?> GetArcadeReplay(string hash, CancellationToken token = default);
}