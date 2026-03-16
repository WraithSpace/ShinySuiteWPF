using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace ShinySuite.Services;

/// <summary>
/// Applies GPU-accelerated DWM acrylic blur to the window background.
/// Uses SetWindowCompositionAttribute (ACCENT_ENABLE_ACRYLICBLURBEHIND) which works
/// on Win10 and Win11 with WindowStyle=None + WindowChrome custom-chrome windows.
/// Does NOT require AllowsTransparency — hardware rendering is preserved.
/// </summary>
public static class DwmBackdropService
{
    [DllImport("dwmapi.dll")]
    private static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref Margins pMarInset);

    [DllImport("user32.dll")]
    private static extern bool SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

    [StructLayout(LayoutKind.Sequential)]
    private struct Margins { public int Left, Right, Top, Bottom; }

    [StructLayout(LayoutKind.Sequential)]
    private struct AccentPolicy
    {
        public int AccentState;   // 4 = ACCENT_ENABLE_ACRYLICBLURBEHIND
        public int AccentFlags;
        public int GradientColor; // AABBGGRR tint
        public int AnimationId;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WindowCompositionAttributeData
    {
        public int    Attribute;  // 19 = WCA_ACCENT_POLICY
        public IntPtr Data;
        public int    SizeOfData;
    }

    public static void Apply(Window window)
    {
        // ContentRendered fires after WindowChrome has fully modified the HWND styles,
        // which is required for the acrylic backdrop to attach correctly on frameless windows.
        window.ContentRendered += (_, _) => ApplyToHwnd(new WindowInteropHelper(window).Handle);
    }

    private static unsafe void ApplyToHwnd(IntPtr hwnd)
    {
        // Extend DWM glass frame over entire client area
        var margins = new Margins { Left = -1, Right = -1, Top = -1, Bottom = -1 };
        DwmExtendFrameIntoClientArea(hwnd, ref margins);

        // SetWindowCompositionAttribute works on both Win10 and Win11 with custom chrome.
        var accent = new AccentPolicy
        {
            AccentState   = 4,    // ACCENT_ENABLE_ACRYLICBLURBEHIND
            AccentFlags   = 0x20, // draw gradient
            GradientColor = unchecked((int)0x01000000), // AABBGGRR: nearly transparent, no tint
        };
        var data = new WindowCompositionAttributeData
        {
            Attribute  = 19, // WCA_ACCENT_POLICY
            Data       = (IntPtr)(&accent),
            SizeOfData = sizeof(AccentPolicy),
        };
        SetWindowCompositionAttribute(hwnd, ref data);
    }
}
