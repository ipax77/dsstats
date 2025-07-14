namespace dsstats.builder;

public static class BuildPlayer
{
    public static void ReplayInput(List<InputEvent> events)
    {
        Console.WriteLine("Replaying events...");

        bool shiftCurrentlyDown = false;
        bool mouseCurrentlyDown = false;
        bool ctrlCurrentlyDown = false;
        bool mouseMiddleCurrentlyDown = false;

        foreach (var e in events)
        {
            Thread.Sleep(e.DelayMs);

            // --- Handle Shift state ---
            if (e.ShiftKeyDown && !shiftCurrentlyDown)
            {
                User32Wrapper.keybd_event(User32Wrapper.VK_SHIFT, 0, 0, UIntPtr.Zero);
                shiftCurrentlyDown = true;
            }
            else if (!e.ShiftKeyDown && shiftCurrentlyDown)
            {
                User32Wrapper.keybd_event(User32Wrapper.VK_SHIFT, 0, User32Wrapper.KEYEVENTF_KEYUP, UIntPtr.Zero);
                shiftCurrentlyDown = false;
            }

            // --- Handle Ctrl state ---
            if (e.CtrlKeyDown && !ctrlCurrentlyDown)
            {
                User32Wrapper.keybd_event(User32Wrapper.VK_CONTROL, 0, 0, UIntPtr.Zero);
                ctrlCurrentlyDown = true;
            }
            else if (!e.CtrlKeyDown && ctrlCurrentlyDown)
            {
                User32Wrapper.keybd_event(User32Wrapper.VK_CONTROL, 0, User32Wrapper.KEYEVENTF_KEYUP, UIntPtr.Zero);
                ctrlCurrentlyDown = false;
            }

            // --- Handle Left Mouse Button state ---
            if (e.LeftMouseDown && !mouseCurrentlyDown)
            {
                User32Wrapper.mouse_event(User32Wrapper.MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
                mouseCurrentlyDown = true;
            }
            else if (!e.LeftMouseDown && mouseCurrentlyDown)
            {
                User32Wrapper.mouse_event(User32Wrapper.MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
                mouseCurrentlyDown = false;
            }

            if (e.MiddleMouseDown && !mouseMiddleCurrentlyDown)
            {
                User32Wrapper.mouse_event(User32Wrapper.MOUSEEVENTF_MIDDLEDOWN, 0, 0, 0, UIntPtr.Zero);
                mouseMiddleCurrentlyDown = true;
            }
            else if (!e.MiddleMouseDown && mouseMiddleCurrentlyDown)
            {
                User32Wrapper.mouse_event(User32Wrapper.MOUSEEVENTF_MIDDLEUP, 0, 0, 0, UIntPtr.Zero);
                mouseMiddleCurrentlyDown = false;
            }

            if (e.Type == InputType.MouseClick)
            {
                // Move mouse (optional)
                User32Wrapper.SetCursorPos(e.X, e.Y);
                if (!e.LeftMouseDown)
                {
                    User32Wrapper.mouse_event(User32Wrapper.MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
                    User32Wrapper.mouse_event(User32Wrapper.MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
                }
            }
            else if (e.Type == InputType.KeyPress)
            {
                User32Wrapper.keybd_event((byte)e.KeyCode, 0, 0, UIntPtr.Zero); // Key down
                User32Wrapper.keybd_event((byte)e.KeyCode, 0, User32Wrapper.KEYEVENTF_KEYUP, UIntPtr.Zero); // Key up
            }
            else if (e.Type == InputType.MouseMove)
            {
                User32Wrapper.SetCursorPos(e.X, e.Y);
            }
            else if (e.Type == InputType.MouseMoveRelative)
            {
                User32Wrapper.SimulateRelativeMouseMove(e.X, e.Y);
            }
            else if (e.Type == InputType.MouseRightClick)
            {
                User32Wrapper.SetCursorPos(e.X, e.Y);
                User32Wrapper.mouse_event(User32Wrapper.MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, UIntPtr.Zero);
                User32Wrapper.mouse_event(User32Wrapper.MOUSEEVENTF_RIGHTUP, 0, 0, 0, UIntPtr.Zero);
            }
        }
        if (shiftCurrentlyDown)
            User32Wrapper.keybd_event(User32Wrapper.VK_SHIFT, 0, User32Wrapper.KEYEVENTF_KEYUP, UIntPtr.Zero);

        if (mouseCurrentlyDown)
            User32Wrapper.mouse_event(User32Wrapper.MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);


        Console.WriteLine("Replay complete.");
    }
}