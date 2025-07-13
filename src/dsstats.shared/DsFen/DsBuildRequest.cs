namespace dsstats.shared.DsFen;

public record DsBuildRequest
{
    public Commander Commander { get; set; }
    public int Team { get; set; }
    public List<PlayerUpgradeDto> Upgrades { get; set; } = [];
    public SpawnDto Spawn { get; set; } = new();
    public bool Mirror { get; set; } = false;
}
