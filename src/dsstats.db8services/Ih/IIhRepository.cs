using dsstats.shared;

namespace dsstats.db8services
{
    public interface IIhRepository
    {
        Task<GroupStateV2> GetOrCreateGroupState(Guid groupId, RatingType ratingType = RatingType.StdTE);
        Task UpdateGroupState(GroupStateV2 groupState);
        Task<List<GroupStateDto>> GetOpenGroups();
        Task CloseGroup(Guid groupId);
        Task<List<ReplayListDto>> GetReplays(Guid groupId);
        Task CalculatePerformance(GroupStateV2 groupState);
        Task ArchiveSession(Guid groupId);
        Task ArchiveV1();
        Task<List<IhSessionListDto>> GetIhSessions(int skip, int take, CancellationToken token);
        Task<IhSessionDto?> GetIhSession(Guid groupId);
    }
}