namespace dsstats.builder;

public static class DsBuilder
{
    const int DelayMs = 5;

    public static List<InputEvent> BuildArmy(string commander, int screenWidth, int screenHeight, bool dry = true)
    {
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
            events.AddRange(EnterString("Clear"));
            events.AddRange(EnterString($"Enemy {commander}"));

            // rebind worker
            events.Add(new(InputType.MouseClick, screenWidth / 2, screenHeight / 2, 0, DelayMs)); // TODO
            events.Add(new(InputType.KeyPress, 0, 0, 0x32, DelayMs, false, false, true));
        }

        events.Add(new(InputType.KeyPress, 0, 0, 0x51, DelayMs));
        // build zerglings
        events.AddRange(BuildTestUnits(screenWidth, screenHeight));

        return events;
    }

    private static List<InputEvent> BuildTestUnits(int screenWidth, int screenHeight)
    {
        // y < 15 -> need to scroll top
        // y > 1140 -> need to scroll bottom

        // team 2
        // Left: 73,82 Right: 101,76 Bottom: 90,65 Top: 84,93

        int team = 2;
        string swarmlings = "90,75,90,82,83,82,83,75,84,75,85,75,86,75,87,75,88,75,89,75,90,76,90,77,90,78,90,79,90,80,90,81,89,82,88,82,87,82,86,82,85,82,84,82,83,81,83,80,83,79,83,78,83,77,83,76,91,83,91,82,91,81,91,80,91,79,91,78,91,77,91,76,91,75,91,74,90,74,89,74,88,74,87,74,86,74,85,74,84,74,83,74,82,74,82,75,82,76,82,77,82,78,82,79,82,80,82,81,82,82,82,83,83,83,84,83,85,83,86,83,87,83,88,83,89,83,90,83,81,74,81,75,81,76,81,77,81,78,81,79,81,80,81,81,81,82,81,83,81,84,82,84,83,84,84,84,85,84,86,84,87,84,88,84,89,84,90,84,91,84,92,84,92,83,92,82,92,81,92,80,92,79,92,78,92,77,92,76,92,75,92,74,92,73,91,73,90,73,89,73,88,73,87,73,86,73,85,73,84,73,83,73,82,73,83,72,84,72,85,72,86,72,87,72,88,72,89,72,90,72,91,72,92,72,93,72,93,73,93,74,93,75,93,76,93,77,93,78,93,79,93,80,93,81,93,82,93,83,93,84,92,85,91,85,90,85,89,85,88,85,87,85,86,85,85,85,84,85,83,85,82,85,81,85,80,85,80,84,80,83,80,82,80,81,80,80,80,79,80,78,80,77,80,76,80,75,79,76,79,77,79,78,79,79,79,80,79,81,79,82,79,83,79,84,79,85,79,86,80,86,81,86,82,86,83,86,84,86,85,86,86,86,87,86,88,86,89,86,90,86,91,86,94,83,94,82,94,81,94,80,94,79,94,78,94,77,94,76,94,75,94,74,94,73,94,72,94,71,93,71,92,71,91,71,90,71,89,71,88,71,87,71,86,71,84,71,85,71,85,70,86,70,87,70,88,70,89,70,90,70,91,70,92,70,93,70,94,70,95,70,95,71,95,72,96,73,95,73,95,74,95,75,95,76,95,77,95,79,95,78,95,80,95,81,95,82,96,81,96,80,96,71,97,72,96,72,98,73,97,73,99,74,98,74,97,74,96,74,96,75,97,75,98,75,99,75,100,75,96,76,97,76,98,76,99,76,100,76,101,76,100,77,99,77,98,77,97,77,96,77,96,78,96,79,97,80,98,79,99,78,98,78,97,78,97,79,86,69,87,68,87,69,88,68,88,67,88,69,89,69,89,68,89,67,89,66,90,65,90,66,91,66,90,67,91,67,92,67,93,68,92,68,91,68,90,68,94,69,93,69,92,69,91,69,90,69,78,77,77,78,78,78,78,79,77,79,76,79,75,80,76,80,77,80,78,80,78,81,77,81,76,81,75,81,74,81,78,82,77,82,76,82,75,82,74,82,73,82,74,83,75,83,76,83,77,83,78,83,78,84,77,84,76,84,75,84,76,85,77,85,78,85,78,86,77,86,78,87,79,87,80,87,79,88,80,88,81,87,82,87,80,89,81,88,81,89,82,88,83,87,84,87,83,88,83,89,82,89,81,90,82,90,82,91,83,90,84,89,84,88,85,88,85,87,86,87,87,87,88,87,89,87,90,87,89,88,88,88,87,88,86,88,88,89,87,90,87,89,86,91,86,90,86,89,85,89,85,90,84,90,83,91,83,92,84,93,84,92,84,91,85,91,85,92";
        string banelings = "88,77,88,80,85,80,85,77,84,76,89,81,89,76,84,81,86,76,87,76,84,78,84,79,86,81,87,81,89,79,89,78,88,76,87,77,86,77,85,76,89,77,84,77,85,78,87,80,88,78,88,79,85,79,86,80,85,81,89,80,88,81,84,80";

        HashSet<RlPoint> rlPoints = [];

        var buildArea = new BuildArea();
        buildArea.PlaceUnits("Zergling", swarmlings, team);
        buildArea.PlaceUnits("Baneling", banelings, team);

        var screenArea = new ScreenArea(2560, 1440);
        var events = buildArea.GetBuildEvents(screenArea);

        return events;
    }

    private static List<InputEvent> ScrollCenter(int key = 0x31)
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

    private static List<InputEvent> ScrollY(int offsetY, RlPoint center)
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

    private static List<InputEvent> EnterString(string msg)
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