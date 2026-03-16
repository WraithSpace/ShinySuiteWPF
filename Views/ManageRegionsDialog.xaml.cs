using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ShinySuite.Models;
using ShinySuite.Services;
using ShinySuite.ViewModels;

namespace ShinySuite.Views;

public partial class ManageRegionsDialog : Window
{
    private readonly RouteViewModel _route;
    // Display strings bound to the ListBox
    private readonly ObservableCollection<string> _items = [];

    public ManageRegionsDialog(RouteViewModel route)
    {
        InitializeComponent();
        DarkTitleBar.Apply(this);
        _route = route;
        RegionsList.ItemsSource = _items;
        Refresh();
    }

    private void Refresh()
    {
        _items.Clear();
        foreach (var r in _route.OcrRegions)
            _items.Add($"X={r.X}  Y={r.Y}  W={r.W}  H={r.H}");
    }

    private void DeleteRegion_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string label)
        {
            int idx = _items.IndexOf(label);
            if (idx >= 0 && idx < _route.OcrRegions.Count)
            {
                _route.OcrRegions.RemoveAt(idx);
                Refresh();
            }
        }
    }

    private void AddRegion_Click(object sender, RoutedEventArgs e)
    {
        var overlay = new ScreenSelectOverlay();
        if (overlay.ShowDialog() == true)
        {
            var r = overlay.SelectedRegion;
            _route.OcrRegions.Add(new Models.OcrRegionConfig { X = r.X, Y = r.Y, W = r.Width, H = r.Height });
            Refresh();
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e)
        => Close();
}
