using dsstats.shared;

namespace dsstats.builder;

public abstract class CmdrBuild
{
    protected Dictionary<string, char> UnitMap = [];
    protected Dictionary<string, char> UpgradeMap = [];
    protected Dictionary<string, char> AbilityMap = [];

    public virtual char? GetUnitChar(string unitName)
    {
        return UnitMap.TryGetValue(unitName, out var c)
            ? c
            : null;
    }

    public virtual char? GetUpgradeChar(string upgradeName)
    {
        return UpgradeMap.TryGetValue(upgradeName, out var c)
            ? c
            : null;
    }

    public virtual char? GetAbilityChar(string abilityName)
    {
        return AbilityMap.TryGetValue(abilityName, out var c)
            ? c
            : null;
    }
}

public static class CmdrBuildFactory
{
    public static CmdrBuild? Create(Commander commander)
    {
        return commander switch
        {
            Commander.Protoss => new ProtossBuild(),
            Commander.Terran => new TerranBuild(),
            Commander.Zerg => new ZergBuild(),
            _ => null
        };
    }
}