using System;
using System.Windows;
using System.Windows.Media.Imaging;
using ShinySuite.Models;
using ShinySuite.Services;
using ShinySuite.ViewModels;


namespace ShinySuite.Views;

public partial class CaptureRegionDialog : Window
{
    private readonly RouteViewModel _route;

    public CaptureRegionDialog(RouteViewModel route)
    {
        InitializeComponent();
        DarkTitleBar.Apply(this);
        _route = route;
    }

    private bool TryGetRegion(out int x, out int y, out int w, out int h)
    {
        x = y = w = h = 0;
        return int.TryParse(TbX.Text, out x)
            && int.TryParse(TbY.Text, out y)
            && int.TryParse(TbW.Text, out w) && w > 0
            && int.TryParse(TbH.Text, out h) && h > 0;
    }

    private void TestCapture_Click(object sender, RoutedEventArgs e)
    {
        if (!TryGetRegion(out int x, out int y, out int w, out int h))
        {
            MessageBox.Show("Enter valid integer coordinates.", "ShinySuite");
            return;
        }
        try
        {
            using var bmp = DetectionService.CaptureRegion(x, y, w, h);
            // Convert System.Drawing.Bitmap → BitmapSource for display
            using var ms = new System.IO.MemoryStream();
            bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            ms.Position = 0;
            var bi = new BitmapImage();
            bi.BeginInit();
            bi.StreamSource = ms;
            bi.CacheOption  = BitmapCacheOption.OnLoad;
            bi.EndInit();
            bi.Freeze();
            PreviewImage.Source = bi;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Capture failed: {ex.Message}", "ShinySuite");
        }
    }

    private void Add_Click(object sender, RoutedEventArgs e)
    {
        if (!TryGetRegion(out int x, out int y, out int w, out int h))
        {
            MessageBox.Show("Enter valid integer coordinates.", "ShinySuite");
            return;
        }
        _route.OcrRegions.Add(new OcrRegionConfig { X = x, Y = y, W = w, H = h });
        DialogResult = true;
    }

    private void SelectRegion_Click(object sender, RoutedEventArgs e)
    {
        // Overlay is Topmost=True so it covers this dialog — no need to Hide/Show
        var overlay = new ScreenSelectOverlay();
        if (overlay.ShowDialog() == true)
        {
            var r = overlay.SelectedRegion;
            _route.OcrRegions.Add(new OcrRegionConfig { X = r.X, Y = r.Y, W = r.Width, H = r.Height });
            DialogResult = true;
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
        => DialogResult = false;
}
