using pax.dsstats.shared;

namespace pax.dsstats.dbng.Services
{
    public interface IStatsService
    {
        Task<StatsResponse> GetCustomTimeline(StatsRequest request);
        Task<StatsResponse> GetCustomWinrate(StatsRequest request);
        Task<StatsResponse> GetStatsResponse(StatsRequest request);
        void ResetStatsCache();
        Task<PlayerDetailDto> GetPlayerDetails(int toonId, CancellationToken token = default);
        Task<ICollection<PlayerMatchupInfo>> GetPlayerDetailInfo(List<int> toonIds, CancellationToken token = default);
        Task<ICollection<PlayerMatchupInfo>> GetPlayerDetailInfo(int toonId, CancellationToken token = default);
        Task<List<CmdrStats>> GetRequestStats(StatsRequest request);
        Task<CrossTableResponse> GetCrossTable(CrossTableRequest request, CancellationToken token = default);
    }
}