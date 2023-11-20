
namespace dsstats.shared.Interfaces;

public interface IBuildService
{
    Task<BuildResponse> GetBuild(BuildRequest request, CancellationToken token = default);
    Task<List<RequestNames>> GetDefaultPlayers();
    Task<List<RequestNames>> GetTopPlayers(RatingType ratingType);
    Task<int> GetReplaysCount(BuildRequest request, CancellationToken token = default);
    Task<List<ReplayListDto>> GetReplays(BuildRequest request, int skip, int take, CancellationToken token);
}