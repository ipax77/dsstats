namespace dsstats.shared.Units;

public sealed class DsUnitInfo
{
    public string UnitName { get; set; } = string.Empty;
    public HashSet<string> Aliases { get; set; } = [];
    public UnitType UnitType { get; set; }
    public UnitSize UnitSize { get; set; }
}

public static class DsUnitInfos
{
    public static readonly DsUnitInfo HornerVikingFighter = new()
    {
        UnitName = "Deimos Viking (Fighter)",
        Aliases = ["HornerDeimosVikingFighter", "DeimosViking"],
        UnitType = UnitType.Air,
        UnitSize = UnitSize.Medium
    };
}

public sealed record UnitBuildInfo(string Name, UnitSize Size, UnitType Type, int Cost, int Life);
public sealed record UnitMapKey(string UnitName, Commander Commander);