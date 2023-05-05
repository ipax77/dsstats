namespace pax.dsstats.shared.Interfaces;

public interface IServerStatsService
{
    Task<List<ServerStatsResult>> GetDsstatsStats();
    Task<List<ServerStatsResult>> GetSc2ArcadeStats();
}