using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using ShinySuite.Models;
using ShinySuite.Services;

namespace ShinySuite.Views;

public partial class HistoryDialog : Window
{
    private readonly List<ShinyEntry> _history;
    private readonly Action _onModified;
    private List<HistoryItem> Items => (List<HistoryItem>)HistoryGrid.ItemsSource;

    public HistoryDialog(List<ShinyEntry> history, Action onModified)
    {
        InitializeComponent();
        DarkTitleBar.Apply(this);
        _history    = history;
        _onModified = onModified;
        Reload();
    }

    private void Reload()
    {
        HistoryGrid.ItemsSource = _history.Select(e => new HistoryItem(e)).ToList();
        UpdateCount();
    }

    private void UpdateCount() =>
        CountBlock.Text = $"{_history.Count} shinies logged";

    private void SelectAllBox_Changed(object sender, RoutedEventArgs e)
    {
        bool check = SelectAllBox.IsChecked == true;
        foreach (var item in Items) item.IsSelected = check;
        HistoryGrid.Items.Refresh();
    }

    private void DeleteSelected_Click(object sender, RoutedEventArgs e)
    {
        var toDelete = Items.Where(i => i.IsSelected).Select(i => i.Entry).ToList();
        if (toDelete.Count == 0) return;
        var result = MessageBox.Show(
            $"Delete {toDelete.Count} selected entr{(toDelete.Count == 1 ? "y" : "ies")}?",
            "Delete Entries", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes) return;
        foreach (var entry in toDelete) _history.Remove(entry);
        SelectAllBox.IsChecked = false;
        Reload();
        _onModified();
    }

    private void DeleteAll_Click(object sender, RoutedEventArgs e)
    {
        if (_history.Count == 0) return;
        var result = MessageBox.Show(
            "Delete all shiny history? This cannot be undone.",
            "Delete All", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes) return;
        _history.Clear();
        SelectAllBox.IsChecked = false;
        Reload();
        _onModified();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}

public class HistoryItem(ShinyEntry entry)
{
    public ShinyEntry Entry      { get; } = entry;
    public bool       IsSelected { get; set; }
    public string Timestamp => Entry.Timestamp;
    public string Pokemon   => Entry.Pokemon;
    public string Route     => Entry.Route;
    public int    Encounters => Entry.Encounters;
    public int    Resets     => Entry.Resets;
}
