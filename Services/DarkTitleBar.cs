using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace ShinySuite.Services;

public static class DarkTitleBar
{
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

    private const int DWMWA_USE_IMMERSIVE_DARK_MODE    = 20;
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE_19 = 19;
    private const int DWMWA_WINDOW_CORNER_PREFERENCE   = 33;
    private const int DWMWCP_ROUND                     = 2;

    public static void Apply(Window window)
    {
        window.SourceInitialized += (_, _) =>
        {
            var hwnd = new WindowInteropHelper(window).Handle;
            int val  = 1;
            DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE,    ref val, sizeof(int));
            DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE_19, ref val, sizeof(int));
            int round = DWMWCP_ROUND;
            DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref round, sizeof(int));
        };
    }
}
