namespace dsstats.db;

public sealed class PlayerUpgrade
{
    public int PlayerUpgradeId { get; set; }
    public int Gameloop { get; set; }
    public int UpgradeId { get; set; }
    public Upgrade? Upgrade { get; set; }
    public int ReplayPlayerId { get; set; }
    public ReplayPlayer? ReplayPlayer { get; set; }
}

