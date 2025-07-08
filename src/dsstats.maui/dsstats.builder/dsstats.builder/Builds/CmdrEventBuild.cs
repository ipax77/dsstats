using dsstats.builder;
using dsstats.shared.DsFen;

public static class CmdrBuildExtensions
{
    public static List<InputEvent> GetBuildEvents(
        this CmdrBuild build,
        string unitName,
        RlPoint pos,
        ScreenArea screenArea,
        WorkerMenu workerMenu)
    {
        var buildOption = build.GetUnitBuildOption(unitName);
        if (buildOption is null)
        {
            return [];
        }

        var unitMap = build.GetUnitMap();
        List<InputEvent> events = [];

        if (buildOption.RequiresToggle)
        {
            if (build.activeUnits.TryGetValue(unitName, out var isActive) && !isActive)
            {
                var toggleEvent = workerMenu.ToggleBuildMenu(buildOption.Key, screenArea);
                if (toggleEvent != null)
                {
                    events.AddRange(toggleEvent);

                    foreach (var other in unitMap.Where(f => f.Value.Key == buildOption.Key && f.Key != unitName))
                    {
                        if (build.activeUnits.ContainsKey(other.Key))
                        {
                            build.activeUnits[other.Key] = false;
                        }
                    }

                    build.activeUnits[unitName] = true;
                }
            }
        }

        events.AddRange(DsBuilder.BuildUnit(buildOption.Key, pos.X, pos.Y));
        return events;
    }
}

