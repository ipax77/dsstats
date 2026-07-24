using dsstats.shared;

namespace dsstats.db;

public class Spawn
{
    public int SpawnId { get; set; }
    public Breakpoint Breakpoint { get; set; }
    public int Income { get; set; }
    public int GasCount { get; set; }
    public int ArmyValue { get; set; }
    public int KilledValue { get; set; }
    public int UpgradeSpent { get; set; }
    public ICollection<SpawnUnit> Units { get; set; } = [];
    public int ReplayPlayerId { get; set; }
    public ReplayPlayer? ReplayPlayer { get; set; }
}
