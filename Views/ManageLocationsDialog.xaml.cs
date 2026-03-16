using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using ShinySuite.Services;
using ShinySuite.ViewModels;

namespace ShinySuite.Views;

public partial class ManageLocationsDialog : Window
{
    private readonly Action<RouteViewModel> _onDelete;
    private readonly Action?               _onResetPrompts;
    private List<RouteItem> Items => (List<RouteItem>)RouteList.ItemsSource;

    public ManageLocationsDialog(IEnumerable<RouteViewModel> routes, Action<RouteViewModel> onDelete,
                                 Action? onResetPrompts = null)
    {
        InitializeComponent();
        DarkTitleBar.Apply(this);
        _onDelete        = onDelete;
        _onResetPrompts  = onResetPrompts;
        RouteList.ItemsSource = routes.Select(r => new RouteItem(r)).ToList();
        if (onResetPrompts is not null)
            ResetPromptsBtn.Visibility = Visibility.Visible;
    }

    private void SelectAllBox_Changed(object sender, RoutedEventArgs e)
    {
        bool check = SelectAllBox.IsChecked == true;
        foreach (var item in Items) item.IsSelected = check;
        RouteList.Items.Refresh();
    }

    private void DeleteSelected_Click(object sender, RoutedEventArgs e)
    {
        var toDelete = Items.Where(i => i.IsSelected).ToList();
        if (toDelete.Count == 0) return;

        var names = string.Join("\n", toDelete.Select(i => $"  • {i.Name}"));
        var dlg = new ConfirmDialog(
            "Delete Locations?",
            $"Permanently delete these locations and all their data?\n\n{names}")
            { Owner = this };
        if (dlg.ShowDialog() != true) return;

        foreach (var item in toDelete)
            _onDelete(item.Route);

        // Refresh the list
        RouteList.ItemsSource = Items.Where(i => !toDelete.Contains(i)).ToList();
        SelectAllBox.IsChecked = false;
    }

    private void ResetPrompts_Click(object sender, RoutedEventArgs e)
    {
        _onResetPrompts?.Invoke();
        ResetPromptsBtn.Visibility = Visibility.Collapsed;
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}

public class RouteItem(RouteViewModel route)
{
    public RouteViewModel Route      { get; } = route;
    public string         Name       { get; } = route.Name;
    public bool           IsSelected { get; set; }
}
