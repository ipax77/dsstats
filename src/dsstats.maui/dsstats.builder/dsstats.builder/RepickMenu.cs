using dsstats.shared;

namespace dsstats.builder;

public static class RepickMenu
{
    // Repick Menu
    // 5 rows, 5 columns
    // Protoss Terran Zerg Empty Empty
    // Stettmann Mengsk Empty Empty Empty
    // Artanis Vorazun Karax Alarak Fenix
    // Raynor Swann Nova Horner Tychus
    // Kerrigan Zagara Abathur Stukov Dehaka

    private static readonly Dictionary<Commander, (int row, int col)> CommanderGridPositions = new()
    {
        [Commander.Protoss] = (0, 0),
        [Commander.Terran] = (0, 1),
        [Commander.Zerg] = (0, 2),
        [Commander.Stetmann] = (1, 0),
        [Commander.Mengsk] = (1, 1),
        [Commander.Artanis] = (2, 0),
        [Commander.Vorazun] = (2, 1),
        [Commander.Karax] = (2, 2),
        [Commander.Alarak] = (2, 3),
        [Commander.Fenix] = (2, 4),
        [Commander.Raynor] = (3, 0),
        [Commander.Swann] = (3, 1),
        [Commander.Nova] = (3, 2),
        [Commander.Horner] = (3, 3),
        [Commander.Tychus] = (3, 4),
        [Commander.Kerrigan] = (4, 0),
        [Commander.Zagara] = (4, 1),
        [Commander.Abathur] = (4, 2),
        [Commander.Stukov] = (4, 3),
        [Commander.Dehaka] = (4, 4)
    };

    private static RlPoint topLeft = new(2063, 948);
    private static RlPoint bottomRight = new(2512, 1400);


    // TODO: Stettmann & Mengsk (no hotkey)

    public static List<InputEvent> PickCommander(Commander commander, int team, ScreenArea screenArea, bool dry = false)
    {
        var events = new List<InputEvent>();
        int worker = team == 1 ? 0x31 : 0x32;
        if (commander == Commander.None) return events;

        if (team == 1)
        {

            var screenPoint = GetCommanderClickPosition(commander);
            if (screenPoint is null)
            {
                return events;
            }
            var relativeScreenPoint = screenArea.ApplyTransforms(screenPoint);
            events.AddRange(DsBuilder.EnterString("Repick"));
            events.Add(new InputEvent(InputType.MouseClick, relativeScreenPoint.X, relativeScreenPoint.Y, 0, 200));
        }
        else
        {
            events.AddRange(DsBuilder.EnterString($"Enemy {commander}"));
        }
        // rebind worker
        if (!dry)
        {
            var center = screenArea.GetCenter();
            events.Add(new(InputType.MouseClick, center.X, center.Y, 0, 100));
            events.Add(new(InputType.KeyPress, 0, 0, worker, 200, false, false, true));
        }
        return events;
    }

    private static RlPoint? GetCommanderClickPosition(Commander commander)
    {
        if (!CommanderGridPositions.TryGetValue(commander, out var pos))
            return null;

        int cellWidth = (bottomRight.X - topLeft.X) / 5;
        int cellHeight = (bottomRight.Y - topLeft.Y) / 5;

        int x = topLeft.X + pos.col * cellWidth + cellWidth / 2;
        int y = topLeft.Y + pos.row * cellHeight + cellHeight / 2;

        return new RlPoint(x, y);
    }
}