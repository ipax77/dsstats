namespace dsstats.db;

public class SpawnUnit
{
    public int SpawnUnitId { get; set; }
    public int Count { get; set; }
    public int[] Positions { get; set; } = [];
    public int UnitId { get; set; }
    public Unit? Unit { get; set; }
    public int SpawnId { get; set; }
    public Spawn? Spawn { get; set; }
}
