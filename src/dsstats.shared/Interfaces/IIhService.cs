
namespace dsstats.shared.Interfaces;

public interface IIhService
{
    Task<PlayerStateV2?> AddPlayerToGroup(Guid groupId, RequestNames requestNames, bool dry = false);
    Task<GroupStateV2> CreateOrVisitGroup(Guid groupId);
    Task<GroupStateV2?> GetDecodeResultAsync(Guid guid);
    Task<List<GroupStateDto>> GetOpenGroups();
    GroupStateV2? LeaveGroup(Guid groupId);
    Task<PlayerStateV2?> RemovePlayerFromGroup(Guid groupId, RequestNames requestNames);
    Task<bool> AddPlayerToQueue(Guid groupId, PlayerId playerId);
    Task<bool> RemovePlayerFromQueue(Guid groupId, PlayerId playerId);
    Task<List<ReplayListDto>> GetReplays(Guid groupId);
    Task<GroupStateV2?> CalculatePerformance(Guid guid);
    Task Cleanup();
}