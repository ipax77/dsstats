namespace dsstats.builder;

public abstract class CmdrBuild
{
    protected Dictionary<string, BuildOption> UnitMap = [];
    protected Dictionary<string, BuildOption> UpgradeMap = [];
    protected Dictionary<string, BuildOption> AbilityMap = [];
    protected WorkerMenu WorkerMenu = new();
    protected Dictionary<string, bool> activeUnits = [];

    protected void CreateActiveUnits()
    {
        activeUnits = UnitMap.Where(x => x.Value.RequiresToggle).ToDictionary(k => k.Key, v => v.Value.IsActive);
    }

    public virtual List<InputEvent> GetBuildEvents(string unitName, RlPoint pos, ScreenArea screenArea)
    {
        if (!UnitMap.TryGetValue(unitName, out var buildOption)
            || buildOption is null)
        {
            return [];
        }
        List<InputEvent> events = [];
        if (buildOption.RequiresToggle)
        {
            if (activeUnits.TryGetValue(unitName, out var isActive))
            {
                if (!isActive)
                {
                    var toggleEvent = WorkerMenu.ToggleBuildMenu(buildOption.Key, screenArea);
                    if (toggleEvent != null)
                    {
                        events.AddRange(toggleEvent);
                        var otherToggleUnits = UnitMap.Where(f => f.Value.Key == buildOption.Key && f.Key != unitName);
                        foreach (var otherUnit in otherToggleUnits)
                        {
                            if (activeUnits.ContainsKey(otherUnit.Key))
                            {
                                activeUnits[otherUnit.Key] = false;
                            }
                        }
                        activeUnits[unitName] = true;
                    }
                }
            }
        }
        events.AddRange(DsBuilder.BuildUnit(buildOption.Key, pos.X, pos.Y));
        return events;
    }

    public virtual char? GetUnitChar(string unitName)
    {
        return UnitMap.TryGetValue(unitName, out var buildOption)
            ? buildOption.Key
            : null;
    }

    public virtual char? GetUpgradeChar(string upgradeName)
    {
        return UpgradeMap.TryGetValue(upgradeName, out var buildOption)
            ? buildOption.Key
            : null;
    }

    public virtual char? GetAbilityChar(string abilityName)
    {
        return AbilityMap.TryGetValue(abilityName, out var buildOption)
            ? buildOption.Key
            : null;
    }
}

public sealed record BuildOption(char Key, bool RequiresToggle = false, bool IsActive = false, bool IsAbility = false);