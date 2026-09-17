namespace dsstats.shared.Interfaces;

public interface IPlayerProfileService
{
    Task<PlayerOverview?> GetOverview(PlayerProfileRequest request, CancellationToken token = default);
    Task<PlayerProfileDetails?> GetDetails(PlayerProfileRequest request, CancellationToken token = default);
    Task<CmdrAvgGainResponse> GetCommanderPerformance(PlayerStatsRequest request, CancellationToken token = default);
}
