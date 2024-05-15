using dsstats.shared;

namespace dsstats.db8services
{
    public interface IIhRepository
    {
        Task<GroupState> GetOrCreateGroupState(Guid groupId, RatingType ratingType = RatingType.StdTE);
        Task UpdateGroupState(GroupState groupState);
        Task<List<GroupStateDto>> GetOpenGroups();
        Task CloseGroup(Guid groupId);
        Task<List<ReplayListDto>> GetReplays(Guid groupId);
        Task CalcultePerformance(GroupState groupState);
    }
}