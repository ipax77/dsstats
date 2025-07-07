using System.Collections.Frozen;
using System.Runtime.InteropServices;

namespace dsstats.builder;

public static class User32Wrapper
{
    [DllImport("user32.dll")]
    public static extern bool GetCursorPos(out POINT lpPoint);
    [DllImport("user32.dll")]
    public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

    [DllImport("user32.dll")]
    public static extern short GetAsyncKeyState(int vKey);

    [DllImport("user32.dll")]
    public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);
    [DllImport("user32.dll")]
    public static extern bool SetCursorPos(int X, int Y);
    [DllImport("user32.dll")]
    public static extern int GetSystemMetrics(int nIndex);
    [DllImport("user32.dll", SetLastError = true)]
    static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;
    }
    [StructLayout(LayoutKind.Sequential)]
    struct INPUT
    {
        public uint type;
        public InputUnion u;
    }

    [StructLayout(LayoutKind.Explicit)]
    struct InputUnion
    {
        [FieldOffset(0)]
        public MOUSEINPUT mi;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    public static void SimulateRelativeMouseMove(int deltaX, int deltaY)
    {
        INPUT[] inputs = new INPUT[1];

        inputs[0].type = INPUT_MOUSE;
        inputs[0].u.mi = new MOUSEINPUT
        {
            dx = deltaX,
            dy = deltaY,
            mouseData = 0,
            dwFlags = MOUSEEVENTF_MOVE,
            time = 0,
            dwExtraInfo = IntPtr.Zero
        };

        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
    }

    // Mouse event constants
    const uint INPUT_MOUSE = 0;
    const uint MOUSEEVENTF_MOVE = 0x0001;
    public const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    public const uint MOUSEEVENTF_LEFTUP = 0x0004;
    public const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
    public const uint MOUSEEVENTF_MIDDLEUP = 0x0040;
    public const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
    public const uint MOUSEEVENTF_RIGHTUP = 0x0010;

    // Keyboard event constants
    public const uint KEYEVENTF_KEYUP = 0x0002;
    public const int SM_CXSCREEN = 0;
    public const int SM_CYSCREEN = 1;
    public const int VK_SPACE = 0x20;
    public const int VK_SHIFT = 0x10;
    public const int VK_RETURN = 0x0D;
    public const int VK_CONTROL = 0x11;
    public const int VK_LEFT = 0x25; // 37
    public const int VK_UP = 0x26; // 38
    public const int VK_RIGHT = 0x27; // 39
    public const int VK_DOWN = 0x28; // 40
    public const int VK_PRIOR = 0x21; // 33 Page up key
    public static List<int> LetterKeys => LetterDict.Values.ToList();
    public static List<int> NumberKeys => NumberDict.Values.ToList();
    private static FrozenDictionary<char, int> NumberDict = new Dictionary<char, int>()
    {
        {'0',   0x30 },  //	0 key 48
        {'1',   0x31 },  //	1 key 49
        {'2',   0x32 },  //	2 key 50
        {'3',   0x33 },  //	3 key 51
        {'4',   0x34 },  //	4 key 52
        {'5',   0x35 },  //	5 key 53
        {'6',   0x36 },  //	6 key 54
        {'7',   0x37 },  //	7 key 55
        {'8',   0x38 },  //	8 key 56
        {'9',   0x39 },  //	9 key 57
    }.ToFrozenDictionary();

    private static FrozenDictionary<char, int> LetterDict = new Dictionary<char, int>()
    {
        {'A',   0x41 },  // A key 65
        {'B',   0x42 },  // B key 66
        {'C',   0x43 },  // C key 67
        {'D',   0x44 },  // D key 68
        {'E',   0x45 },  // E key 69
        {'F',   0x46 },  // F key 70
        {'G',   0x47 },  // G key 71
        {'H',   0x48 },  // H key 72
        {'I',   0x49 },  // I key 73
        {'J',   0x4A },  // J key 74
        {'K',   0x4B },  // K key 75
        {'L',   0x4C },  // L key 76
        {'M',   0x4D },  // M key 77
        {'N',   0x4E },  // N key 78
        {'O',   0x4F },  // O key 79
        {'P',   0x50 },  // P key 80
        {'Q',   0x51 },  // Q key 81
        {'R',   0x52 },  // R key 82
        {'S',   0x53 },  // S key 83
        {'T',   0x54 },  // T key 84
        {'U',   0x55 },  // U key 85
        {'V',   0x56 },  // V key 86
        {'W',   0x57 },  // W key 87
        {'X',   0x58 },  // X key 88
        {'Y',   0x59 },  // Y key 89
        {'Z',   0x5A },  // Z key 90
    }.ToFrozenDictionary();

    public static bool TryMapCharToKey(char c, out int vkCode, out bool requiresShift)
    {
        requiresShift = false;
        vkCode = 0;

        if (char.IsLetter(c))
        {
            requiresShift = char.IsUpper(c);
            if (LetterDict.TryGetValue(char.ToUpper(c), out var vk))
            {
                vkCode = vk;
                return true;
            }
            return false;
        }

        if (char.IsDigit(c))
        {
            if (NumberDict.TryGetValue(c, out var vk))
            {
                vkCode = vk;
                return true;
            }
            return false;
        }

        if (c == ' ')
        {
            vkCode = VK_SPACE;
            return true;
        }

        vkCode = 0;
        return false;
    }

}