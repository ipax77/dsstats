using dsstats.shared;

namespace dsstats.db;

public sealed class Spawn
{
    public int SpawnId { get; set; }
    public int Gameloop { get; set; }
    public Breakpoint Breakpoint { get; set; }
    public int Income { get; set; }
    public int GasCount { get; set; }
    public int ArmyValue { get; set; }
    public int KilledValue { get; set; }
    public int UpgradeSpent { get; set; }
    public int ReplayPlayerId { get; set; }
    public ReplayPlayer? ReplayPlayer { get; set; }
    public ICollection<SpawnUnit> SpawnUnits { get; set; } = [];
}

