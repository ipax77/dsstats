using dsstats.shared;
using dsstats.shared.DsFen;

namespace dsstats.builder;

public static class DsBuilder
{
    const int DelayMs = 10;

    public static List<InputEvent> BuildArmy(Commander commander, int screenWidth, int screenHeight, bool dry = true)
    {
        int team = 2;
        List<InputEvent> events = [];

        if (!dry)
        {
            // zoom out
            events.Add(new(InputType.KeyPress, 0, 0, User32Wrapper.VK_PRIOR, DelayMs));
            events.Add(new(InputType.KeyPress, 0, 0, User32Wrapper.VK_PRIOR, DelayMs));
            events.Add(new(InputType.KeyPress, 0, 0, User32Wrapper.VK_PRIOR, DelayMs));
            events.Add(new(InputType.KeyPress, 0, 0, User32Wrapper.VK_PRIOR, DelayMs));
            events.Add(new(InputType.KeyPress, 0, 0, User32Wrapper.VK_PRIOR, DelayMs));

            // center worker
            events.Add(new(InputType.KeyPress, 0, 0, 0x31, DelayMs));
            events.AddRange(ScrollCenter());

            // setup
            events.AddRange(EnterString("Infinite"));
            events.AddRange(EnterString($"Tier"));
            // events.AddRange(EnterString("Clear"));
            var screenArea = new ScreenArea(team, screenWidth, screenHeight);
            events.AddRange(RepickMenu.PickCommander(Commander.Terran, team, screenArea, dry));
        }

        events.Add(new(InputType.KeyPress, 0, 0, 0x51, DelayMs));
        // build zerglings
        events.AddRange(BuildTestUnits(team, screenWidth, screenHeight));

        return events;
    }

    public static void Build(DsBuildRequest buildRequest, bool dry = false)
    {
        Thread.Sleep(500);
        int team = buildRequest.Team;
        if (buildRequest.Mirror)
        {
            team = team == 1 ? 2 : 1;
        }
        var build = CmdrBuildFactory.Create(buildRequest.Commander);

        if (build is null)
        {
            return;
        }
        int screenWidth = User32Wrapper.GetSystemMetrics(User32Wrapper.SM_CXSCREEN);
        int screenHeight = User32Wrapper.GetSystemMetrics(User32Wrapper.SM_CYSCREEN);
        var screenArea = new ScreenArea(team, screenWidth, screenHeight);

        List<InputEvent> events = [];
        if (!dry)
        {
            events.AddRange(Setup(buildRequest.Commander, team, screenArea));
        }

        // build
        events.Add(new(InputType.KeyPress, 0, 0, 0x51, DelayMs));

        events.AddRange(GetBuildEvents(buildRequest.Spawn, build, team, screenArea, buildRequest.Mirror));


        // upgrades
        int workerKey = buildRequest.Team == 1 ? 0x31 : 0x32;
        List<PlayerUpgradeDto> upgrades = new(buildRequest.Upgrades);
        events.Add(new(InputType.KeyPress, 0, 0, workerKey, 100));
        events.Add(new(InputType.KeyPress, 0, 0, 0x57, 100));
        foreach (var upgrade in buildRequest.Upgrades)
        {
            var upgradeChar = build.GetAbilityChar(upgrade.Upgrade.Name);
            if (upgradeChar is not null
                && User32Wrapper.TryMapCharToKey(upgradeChar.Value, out var code, out var shift))
            {
                events.Add(new InputEvent(InputType.KeyPress, 0, 0, code, 200));
                upgrades.Remove(upgrade);
            }
        }
        events.Add(new(InputType.KeyPress, 0, 0, workerKey, 100));
        foreach (var upgrade in upgrades)
        {
            var upgradeChar = build.GetUpgradeChar(upgrade.Upgrade.Name);
            if (upgradeChar is not null
                && User32Wrapper.TryMapCharToKey(upgradeChar.Value, out var code, out var shift))
            {
                events.Add(new InputEvent(InputType.KeyPress, 0, 0, code, 200));
            }
        }

        BuildPlayer.ReplayInput(events);
    }

    private static List<InputEvent> Setup(Commander commander, int team, ScreenArea screenArea)
    {
        int workerKey = team == 1 ? 0x31 : 0x32;
        List<InputEvent> events = [];

        // zoom out
        events.Add(new(InputType.KeyPress, 0, 0, User32Wrapper.VK_PRIOR, DelayMs));
        events.Add(new(InputType.KeyPress, 0, 0, User32Wrapper.VK_PRIOR, DelayMs));
        events.Add(new(InputType.KeyPress, 0, 0, User32Wrapper.VK_PRIOR, DelayMs));
        events.Add(new(InputType.KeyPress, 0, 0, User32Wrapper.VK_PRIOR, DelayMs));
        events.Add(new(InputType.KeyPress, 0, 0, User32Wrapper.VK_PRIOR, DelayMs));

        // center worker
        events.Add(new(InputType.KeyPress, 0, 0, workerKey, DelayMs));
        events.AddRange(ScrollCenter(workerKey));

        // setup
        events.AddRange(EnterString("Infinite"));
        events.AddRange(EnterString("Tier"));
        // events.AddRange(EnterString("Clear"));
        events.AddRange(RepickMenu.PickCommander(commander, team, screenArea));
        return events;
    }

    private static List<InputEvent> GetBuildEvents(SpawnDto spawn, CmdrBuild build, int team, ScreenArea screenArea, bool mirror = false)
    {
        int buildTeam = team;
        if (mirror)
        {
            buildTeam = team == 1 ? 2 : 1;
        }
        var buildArea = new BuildArea(buildTeam);

        foreach (var unit in spawn.Units)
        {
            buildArea.PlaceUnits(unit.Unit.Name, unit.Poss, buildTeam);
        }
        return buildArea.GetBuildEvents(screenArea, build, mirror);
    }

    private static List<InputEvent> BuildTestUnits(int team, int screenWidth, int screenHeight)
    {
        // y < 15 -> need to scroll top
        // y > 1140 -> need to scroll bottom

        // team 2
        // Left: 73,82 Right: 101,76 Bottom: 90,65 Top: 84,93

        string swarmlings = "90,79,93,79,90,82,96,76,99,76,96,79,93,76,96,73,87,88,93,82,84,91,84,88,90,85,87,85,87,82,84,85,81,88,93,73,90,76,87,79,84,82,81,85,78,85,81,82,84,79,87,76,90,73,93,70,78,82,81,79,84,76,87,73,90,70,90,67,75,82,84,73,87,70,81,76,78,79";

        var build = new TerranBuild();
        var buildArea = new BuildArea(team);
        buildArea.PlaceUnits("Thor", swarmlings, team);

        var screenArea = new ScreenArea(team, 2560, 1440);
        var events = buildArea.GetBuildEvents(screenArea, build);

        return events;
    }

    public static List<InputEvent> ScrollCenter(int key = 0x31)
    {
        List<InputEvent> events = [];
        events.Add(new(InputType.KeyPress, 0, 0, key, DelayMs));
        events.Add(new(InputType.KeyPress, 0, 0, key, DelayMs));
        return events;
    }

    public static List<InputEvent> BuildUnit(char c, int x, int y)
    {
        List<InputEvent> events = [];
        if (User32Wrapper.TryMapCharToKey(c, out var vkCode, out var requiresShift))
        {
            events.Add(new(InputType.MouseMove, x, y, 0, DelayMs));
            events.Add(new(InputType.KeyPress, 0, 0, vkCode, DelayMs, requiresShift));
            events.Add(new(InputType.MouseClick, x, y, 0, DelayMs));
        }
        return events;
    }

    public static List<InputEvent> ScrollY(int offsetY, RlPoint center)
    {
        List<InputEvent> events = [];
        events.Add(new(InputType.MouseMove, center.X, center.Y, 0, DelayMs));

        int step = 2; // You can adjust this for finer or coarser steps
        int remaining = Math.Abs(offsetY);
        int direction = Math.Sign(offsetY);

        while (remaining > 0)
        {
            int delta = Math.Min(step, remaining) * direction;
            events.Add(new(InputType.MouseMoveRelative, 0, delta, 0, 1, false, false, false, true));
            remaining -= Math.Abs(delta);
        }
        events.Add(new(InputType.MouseMove, center.X, center.Y, 0, DelayMs));

        return events;
    }

    public static List<InputEvent> EnterString(string msg)
    {
        List<InputEvent> events = [];
        events.Add(new(InputType.KeyPress, 0, 0, User32Wrapper.VK_RETURN, DelayMs));
        for (int i = 0; i < msg.Length; i++)
        {
            var c = msg[i];
            if (User32Wrapper.TryMapCharToKey(c, out int vk, out bool requiresShift))
            {
                events.Add(new InputEvent(InputType.KeyPress, 0, 0, vk, DelayMs, requiresShift, false));
            }
        }
        events.Add(new(InputType.KeyPress, 0, 0, User32Wrapper.VK_RETURN, DelayMs));
        return events;
    }
}