
namespace dsstats.shared.Interfaces;

public interface IIhService
{
    Task<PlayerState?> AddPlayerToGroup(Guid groupId, RequestNames requestNames);
    Task<GroupState> CreateOrVisitGroup(Guid groupId);
    Task<GroupState?> GetDecodeResultAsync(Guid guid);
    IhMatch GetIhMatch(IhReplay replay, GroupState groupState);
    Task<List<GroupStateDto>> GetOpenGroups();
    GroupState? LeaveGroup(Guid groupId);
    Task<PlayerState?> RemovePlayerFromGroup(Guid groupId, RequestNames requestNames);
    Task<bool> AddPlayerToQueue(Guid groupId, PlayerId playerId);
    Task<bool> RemovePlayerFromQueue(Guid groupId, PlayerId playerId);
    Task<List<ReplayListDto>> GetReplays(Guid groupId);
    Task<GroupState?> CalculatePerformance(Guid guid);
    Task Cleanup();
}