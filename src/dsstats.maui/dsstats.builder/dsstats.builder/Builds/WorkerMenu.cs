using dsstats.shared;

namespace dsstats.builder;

public class WorkerMenu
{
    // Describes an option grid with 3 rows and 5 columns
    // Column access row1: Q W E R T
    // Column access row2: A S D F G
    // Column access row3: Z/Y X C V B
    private readonly char[,] grid = new char[3, 5]
    {
            { 'q', 'w', 'e', 'r', 't' },
            { 'a', 's', 's', 'f', 'g' },
            { 'z', 'x', 'c', 'v', 'b' }
    };
    private RlPoint topLeft = new(2062, 1135);
    private RlPoint bottomRight = new(2510, 1400);

    public List<InputEvent> ToggleBuildMenu(char c, ScreenArea screenArea)
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
            return [];

        // Calculate grid cell dimensions
        int cellWidth = (bottomRight.X - topLeft.X) / 5;
        int cellHeight = (bottomRight.Y - topLeft.Y) / 3;

        // Compute screen point (center of cell)
        int x = topLeft.X + col * cellWidth + cellWidth / 2;
        int y = topLeft.Y + row * cellHeight + cellHeight / 2;
        RlPoint screenPoint = new(x, y);
        var relativeScreenPoint = screenArea.ApplyTransforms(screenPoint);
        var center = screenArea.GetCenter();
        var toggleEvent = new InputEvent(InputType.MouseRightClick, relativeScreenPoint.X, relativeScreenPoint.Y, 0, 20);
        var moveEvent = new InputEvent(InputType.MouseMove, center.X, center.Y, 0, 200);
        return [toggleEvent, moveEvent];
    }
}