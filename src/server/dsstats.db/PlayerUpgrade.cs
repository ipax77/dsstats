namespace dsstats.db;

public class PlayerUpgrade
{
    public int PlayerUpgradeId { get; set; }
    public int Gameloop { get; set; }
    public int UpgradeId { get; set; }
    public Upgrade? Upgrade { get; set; }
}
