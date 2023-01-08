using Microsoft.UI;
using Microsoft.UI.Windowing;
using pax.dsstats.shared;
using System.Runtime.InteropServices;
using Windows.Graphics;

namespace sc2dsstats.maui;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();

        Microsoft.Maui.Handlers.WindowHandler.Mapper.AppendToMapping(nameof(IWindow), (handler, view) =>
        {
#if WINDOWS
            var nativeWindow = handler.PlatformView;
            nativeWindow.Activate();
            IntPtr windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(nativeWindow);
            ShowWindow(windowHandle, 3);
            WindowId windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(windowHandle);
            AppWindow appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
            var size = appWindow.ClientSize;
            Data.MauiWidth = size.Width;
            Data.MauiHeight = size.Height;
#endif
        });

    }

#if WINDOWS
    [DllImport("user32.dll")]
    public static extern bool ShowWindow(IntPtr hWnd, int cmdShow);
#endif
}
