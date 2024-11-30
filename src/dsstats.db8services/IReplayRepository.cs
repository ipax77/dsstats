using dsstats.db8;
using dsstats.shared;

namespace dsstats.db8services;

public interface IReplayRepository
{
    Task SaveReplay(ReplayDto replayDto,
                    HashSet<Unit> units,
                    HashSet<Upgrade> upgrades);
    Task<ReplayDto?> GetLatestReplay();
    Task<ReplayDto?> GetPreviousReplay(DateTime gameTime);
    Task<ReplayDto?> GetNextReplay(DateTime gameTime);
    Task<ReplayDto?> GetReplay(string replayHash);
    Task SetReplayViews();
    Task SetReplayDownloads();
    Task FixDsstatsPlayerNames();
    Task FixArcadePlayerNames();
}