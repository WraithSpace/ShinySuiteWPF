using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ShinySuite.Views;

public partial class ScreenSelectOverlay : Window
{
    public System.Drawing.Rectangle SelectedRegion { get; private set; }

    private Point _start;
    private bool  _dragging;

    public ScreenSelectOverlay()
    {
        InitializeComponent();

        // Cover the entire virtual desktop (all monitors)
        Left   = SystemParameters.VirtualScreenLeft;
        Top    = SystemParameters.VirtualScreenTop;
        Width  = SystemParameters.VirtualScreenWidth;
        Height = SystemParameters.VirtualScreenHeight;

        KeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape)
                DialogResult = false;
        };
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Mouse handlers
    // ──────────────────────────────────────────────────────────────────────────

    private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;

        _start   = e.GetPosition(OverlayCanvas);
        _dragging = true;

        HintText.Visibility    = Visibility.Collapsed;
        SelectRect.Visibility  = Visibility.Visible;
        CoordBorder.Visibility = Visibility.Visible;

        SelectRect.Width  = 0;
        SelectRect.Height = 0;
        Canvas.SetLeft(SelectRect, _start.X);
        Canvas.SetTop(SelectRect,  _start.Y);

        OverlayCanvas.CaptureMouse();
    }

    private void Canvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_dragging) return;
        Refresh(e.GetPosition(OverlayCanvas), commit: false);
    }

    private void Canvas_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!_dragging) return;

        _dragging = false;
        OverlayCanvas.ReleaseMouseCapture();

        var pos = e.GetPosition(OverlayCanvas);
        double w = Math.Abs(pos.X - _start.X);
        double h = Math.Abs(pos.Y - _start.Y);

        // Ignore tiny accidental clicks
        if (w < 4 || h < 4)
        {
            DialogResult = false;
            return;
        }

        Refresh(pos, commit: true);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Drawing helper
    // ──────────────────────────────────────────────────────────────────────────

    private void Refresh(Point pos, bool commit)
    {
        double x = Math.Min(_start.X, pos.X);
        double y = Math.Min(_start.Y, pos.Y);
        double w = Math.Abs(pos.X - _start.X);
        double h = Math.Abs(pos.Y - _start.Y);

        // Update selection rectangle
        Canvas.SetLeft(SelectRect, x);
        Canvas.SetTop(SelectRect,  y);
        SelectRect.Width  = w;
        SelectRect.Height = h;

        // Position coord label near cursor, clamping to window bounds
        double lx = pos.X + 16;
        double ly = pos.Y + 16;
        if (lx + 190 > Width)  lx = pos.X - 206;
        if (ly + 32  > Height) ly = pos.Y - 48;
        Canvas.SetLeft(CoordBorder, lx);
        Canvas.SetTop(CoordBorder,  ly);

        // Convert WPF logical pixels → physical screen pixels
        var    src = PresentationSource.FromVisual(this);
        double dX  = src?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
        double dY  = src?.CompositionTarget?.TransformToDevice.M22 ?? 1.0;

        int px = (int)((SystemParameters.VirtualScreenLeft + x) * dX);
        int py = (int)((SystemParameters.VirtualScreenTop  + y) * dY);
        int pw = (int)(w * dX);
        int ph = (int)(h * dY);

        CoordText.Text = $"X:{px}  Y:{py}  {pw}×{ph}";

        if (commit)
        {
            SelectedRegion = new System.Drawing.Rectangle(px, py, pw, ph);
            DialogResult   = true;
        }
    }
}
