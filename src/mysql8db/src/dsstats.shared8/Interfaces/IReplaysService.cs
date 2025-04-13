using dsstats.shared;
namespace dsstats.shared8.Interfaces;

public interface IReplaysService
{
    Task<List<ReplayListDto>> GetReplays(ReplaysRequest request, CancellationToken token);
    Task<int> GetReplaysCount(ReplaysRequest request, CancellationToken token);
}