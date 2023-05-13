using pax.dsstats.shared.Arcade;

namespace pax.dsstats.shared.Interfaces;

public interface IServerStatsService
{
    Task<List<ServerStatsResult>> GetDsstatsStats();
    Task<List<ServerStatsResult>> GetSc2ArcadeStats();
    Task<MergeResultReplays> GetMergeResultReplays(PlayerId playerId, CancellationToken token);
}