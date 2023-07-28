
namespace pax.dsstats.shared;

public interface ITeamService
{
    Task<TeamCompResponse> GetTeamRating(TeamCompRequest request, CancellationToken token);
}