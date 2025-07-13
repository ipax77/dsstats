namespace dsstats.builder;

public static class WorkerMenu
{
    private static readonly char[,] grid = new char[3, 5]
    {
        { 'q', 'w', 'e', 'r', 't' },
        { 'a', 's', 's', 'f', 'g' },
        { 'z', 'x', 'c', 'v', 'b' }
    };

    private static RlPoint topLeft = new(2062, 1135);
    private static RlPoint bottomRight = new(2510, 1400);

    public static RlPoint? GetCharPosition(char c, ScreenArea screenArea)
    {
        if (c == 'y') c = 'z';

        int row = -1, col = -1;
        for (int r = 0; r < 3; r++)
        {
            for (int cIdx = 0; cIdx < 5; cIdx++)
            {
                if (grid[r, cIdx] == c)
                {
                    row = r;
                    col = cIdx;
                    break;
                }
            }
            if (row != -1) break;
        }

        if (row == -1 || col == -1)
            return null;

        int cellWidth = (bottomRight.X - topLeft.X) / 5;
        int cellHeight = (bottomRight.Y - topLeft.Y) / 3;

        int x = topLeft.X + col * cellWidth + cellWidth / 2;
        int y = topLeft.Y + row * cellHeight + cellHeight / 2;

        RlPoint screenPoint = new(x, y);
        return screenArea.ApplyTransforms(screenPoint);
    }

    public static List<InputEvent> ToggleBuildMenu(char c, ScreenArea screenArea)
    {
        RlPoint? relativeScreenPoint = GetCharPosition(c, screenArea);
        if (relativeScreenPoint is null)
            return [];
        if (relativeScreenPoint.X == 0 && relativeScreenPoint.Y == 0)
            return [];

        var center = screenArea.GetCenter();
        var moveEvent1 = new InputEvent(InputType.MouseMove, relativeScreenPoint.X, relativeScreenPoint.Y, 0, 200);
        var toggleEvent = new InputEvent(InputType.MouseRightClick, relativeScreenPoint.X, relativeScreenPoint.Y, 0, 20);
        var moveEvent2 = new InputEvent(InputType.MouseMove, center.X, center.Y, 0, 200);
        return [moveEvent1, toggleEvent, moveEvent2];
    }
}
