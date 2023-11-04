using dsstats.db;
using dsstats.shared;

namespace dsstats.services
{
    public interface IReplayRepository
    {
        Task<(HashSet<Unit>, HashSet<Upgrade>, Replay)> SaveReplay(ReplayDto replayDto, HashSet<Unit> units, HashSet<Upgrade> upgrades, ReplayEventDto? replayEventDto);
    }
}