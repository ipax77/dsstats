namespace dsstats.shared.Interfaces;

public interface ITeamcompService
{
    Task<TeamcompResponse> GetTeamcompResult(TeamcompRequest request, CancellationToken token = default);
    Task<int> GetReplaysCount(TeamcompReplaysRequest request, CancellationToken token = default);
    Task<List<ReplayListDto>> GetReplays(TeamcompReplaysRequest request, CancellationToken token);
}
