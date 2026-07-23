namespace dsstats.db;

public class SpawnModification
{
    public int SpawnModificationId { get; set; }
    public int Count { get; set; }
    public int UnitId { get; set; }
    public Unit? Unit { get; set; }
    public int SpawnId { get; set; }
    public Spawn? Spawn { get; set; }
}