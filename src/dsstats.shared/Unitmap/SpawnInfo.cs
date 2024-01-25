namespace dsstats.shared;

public record SpawnRequest
{
    public List<UnitDto> Units { get; set; } = [];
    public Commander Commander { get; set; }
}

public record SpawnInfo
{
    public int ArmyValue { get; init; }
    public int ArmyTotalVitality { get; init; }
    public Dictionary<string, DsUnitBuildDto> BuildUnits { get; init; } = [];
}