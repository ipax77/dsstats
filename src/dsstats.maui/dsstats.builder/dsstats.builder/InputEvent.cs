namespace dsstats.builder;

public sealed record InputEvent(InputType Type, int X, int Y, int KeyCode, int DelayMs,
    bool ShiftKeyDown = false, bool LeftMouseDown = false, bool CtrlKeyDown = false, bool MiddleMouseDown = false);

public enum InputType
{
    None = 0,
    MouseClick = 1,
    MouseMove = 2,
    KeyPress = 3,
    MouseMoveRelative = 4,
    MouseRightClick = 5,
}