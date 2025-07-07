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

    private static RlPoint topLeft = new(2063, 948);
    private static RlPoint bottomRight = new(2512, 1400);


    // TODO: Stettmann & Mengsk (no hotkey)

    public static List<InputEvent> PickCommander(Commander commander, int team, ScreenArea screenArea)
    {
        var events = new List<InputEvent>();
        int worker = team == 1 ? 0x31 : 0x32;
        if (commander == Commander.None) return events;

        if (team == 1)
        {
            // Map commander to grid position
            int row = 0, col = 0;
            switch (commander)
            {
                case Commander.Protoss: row = 0; col = 0; break;
                case Commander.Terran: row = 0; col = 1; break;
                case Commander.Zerg: row = 0; col = 2; break;
                case Commander.Stetmann: row = 1; col = 0; break;
                case Commander.Mengsk: row = 1; col = 1; break;
                case Commander.Artanis: row = 2; col = 0; break;
                case Commander.Vorazun: row = 2; col = 1; break;
                case Commander.Karax: row = 2; col = 2; break;
                case Commander.Alarak: row = 2; col = 3; break;
                case Commander.Fenix: row = 2; col = 4; break;
                case Commander.Raynor: row = 3; col = 0; break;
                case Commander.Swann: row = 3; col = 1; break;
                case Commander.Nova: row = 3; col = 2; break;
                case Commander.Horner: row = 3; col = 3; break;
                case Commander.Tychus: row = 3; col = 4; break;
                case Commander.Kerrigan: row = 4; col = 0; break;
                case Commander.Zagara: row = 4; col = 1; break;
                case Commander.Abathur: row = 4; col = 2; break;
                case Commander.Stukov: row = 4; col = 3; break;
                case Commander.Dehaka: row = 4; col = 4; break;
            }
            // Calculate grid cell dimensions
            int cellWidth = (bottomRight.X - topLeft.X) / 5;
            int cellHeight = (bottomRight.Y - topLeft.Y) / 5;
            // Compute screen point (center of cell)
            int xCenterCellPoint =
                topLeft.X + cellWidth * col + cellWidth / 2;
            int yCenterCellPoint =
                topLeft.Y + cellHeight * row + cellHeight / 2;
            RlPoint screenPoint = new(xCenterCellPoint, yCenterCellPoint);
            var relativeScreenPoint = screenArea.ApplyTransforms(screenPoint);
            events.AddRange(DsBuilder.EnterString("Repick"));
            events.Add(new InputEvent(InputType.MouseClick, relativeScreenPoint.X, relativeScreenPoint.Y, 0, 10));
        }
        else
        {
            events.AddRange(DsBuilder.EnterString($"Enemy {commander}"));
        }
        // rebind worker
        var center = screenArea.GetCenter();
        events.Add(new(InputType.MouseClick, center.X, center.Y, 0, 100));
        events.Add(new(InputType.KeyPress, 0, 0, worker, 200, false, false, true));
        return events;
    }
}