
namespace dsstats.shared.Interfaces;

public interface IIhService
{
    Task<PlayerState?> AddPlayerToGroup(Guid groupId, RequestNames requestNames);
    GroupState? CreateOrVisitGroup(Guid groupId);
    List<IhReplay> GetDecodeResult(Guid guid);
    Task<GroupState?> GetDecodeResultAsync(Guid guid);
    IhMatch GetIhMatch(IhReplay replay, GroupState groupState);
    Task<List<GroupStateDto>> GetOpenGroups();
    GroupState? LeaveGroup(Guid groupId);
    Task<PlayerState?> RemovePlayerFromGroup(Guid groupId, RequestNames requestNames);
    Task<bool> AddPlayerToQueue(Guid groupId, PlayerId playerId);
    Task<bool> RemovePlayerFromQueue(Guid groupId, PlayerId playerId);
}