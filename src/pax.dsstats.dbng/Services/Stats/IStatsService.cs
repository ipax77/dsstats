using pax.dsstats.shared;

namespace pax.dsstats.dbng.Services
{
    public interface IStatsService
    {
        Task<StatsResponse> GetCustomTimeline(StatsRequest request);
        Task<StatsResponse> GetCustomWinrate(StatsRequest request);
        Task<string?> GetPlayerRatings(int toonId);
        Task<List<PlayerRatingDto>> GetRatings(RatingsRequest request, CancellationToken token = default);
        Task<int> GetRatingsCount(RatingsRequest request, CancellationToken token = default);
        Task<List<MmrDevDto>> GetRatingsDeviation();
        Task<List<MmrDevDto>> GetRatingsDeviationStd();
        Task<StatsResponse> GetStatsResponse(StatsRequest request);
        void ResetCache();
        Task<ICollection<PlayerMatchupInfo>> GetPlayerDetailInfo(List<int> toonIds);
        Task<ICollection<PlayerMatchupInfo>> GetPlayerDetailInfo(int toonId);
        Task SeedPlayerInfos();
    }
}