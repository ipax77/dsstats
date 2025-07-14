using dsstats.builder;
using dsstats.shared.DsFen;

public static class CmdrBuildExtensions
{
    public static List<InputEvent> GetBuildEvents(
        this CmdrBuild build,
        string unitName,
        RlPoint screenPos,
        ScreenArea screenArea,
        BuildOption buildOption)
    {
        List<InputEvent> events = [];

        if (buildOption.RequiresToggle)
        {
            if (build.activeUnits.TryGetValue(unitName, out var isActive) && !isActive)
            {
                var toggleEvent = WorkerMenu.ToggleBuildMenu(buildOption.Key, screenArea);
                if (toggleEvent != null)
                {
                    events.AddRange(toggleEvent);

                    foreach (var other in build.GetUnitMap()
                                               .Where(f => f.Value.Key == buildOption.Key && f.Key != unitName))
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

        if (buildOption.IsAbility)
        {
            var worker = screenArea._team == 1 ? 0x31 : 0x32;

            events.Add(new InputEvent(InputType.KeyPress, 0, 0, worker, 10));
            events.Add(new InputEvent(InputType.KeyPress, 0, 0, 0x57, 10)); // W key for ability
            events.AddRange(DsBuilder.BuildUnit(buildOption.Key, screenPos.X, screenPos.Y));
            events.Add(new InputEvent(InputType.KeyPress, 0, 0, worker, 10));
            events.Add(new InputEvent(InputType.KeyPress, 0, 0, 0x51, 10)); // Q key for build menu
        }
        else
        {
            events.AddRange(DsBuilder.BuildUnit(buildOption.Key, screenPos.X, screenPos.Y));
        }

        return events;
    }
}

