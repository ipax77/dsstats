using System;

namespace dsstats.shared;

public record SpawnRequest
{
    public List<UnitDto> Units { get; set; } = [];
    public Commander Commander { get; set; }
}

public record SpawnInfo
{
    public int ArmyValue { get; set; }
    public int ArmyTotalVitality { get; set; }
    public Dictionary<string, DsUnitBuildDto> BuildUnits { get; init; } = [];
}

public static class SpawnInfoExtension
{
    public static void SetArmyStats(this SpawnInfo spawnInfo, SpawnDto spawnDto)
    {
        int spawnArmyValue = 0;
        int spawnArmyLife = 0;

        foreach (var spawnUnit in spawnDto.Units)
        {
            if (spawnInfo.BuildUnits.TryGetValue(spawnUnit.Unit.Name, out var buildUnit)
                && buildUnit is not null)
            {
                spawnArmyValue += buildUnit.Cost * spawnUnit.Count;
                spawnArmyLife += (buildUnit.Life + buildUnit.Shields) * spawnUnit.Count;

            }
        }

        spawnInfo.ArmyValue= spawnArmyValue;
        spawnInfo.ArmyTotalVitality = spawnArmyLife;
    }
}