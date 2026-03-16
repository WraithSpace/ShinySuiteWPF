using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using ShinySuite.Models;
using ShinySuite.Services;
using ShinySuite.ViewModels;

namespace ShinySuite.Views;

public partial class ShinyFoundDialog : Window
{
    private readonly RouteViewModel _route;
    private readonly List<ShinyEntry> _history;

    public ShinyFoundDialog(RouteViewModel route, List<ShinyEntry> history, string accent)
    {
        InitializeComponent();
        DarkTitleBar.Apply(this);
        _route   = route;
        _history = history;

        // Populate pokemon dropdown
        var names = route.Tracked.Values.Select(t => t.DisplayName).ToList();
        PokemonCombo.ItemsSource = names;

        // Pre-select first target if any, otherwise first in list
        var firstTarget = route.Tracked.Values.FirstOrDefault(t => t.IsTarget);
        if (firstTarget is not null)
            PokemonCombo.SelectedItem = firstTarget.DisplayName;
        else if (names.Count > 0)
            PokemonCombo.SelectedIndex = 0;

        StatsBlock.Text = route.StatsText;
    }

    private ShinyEntry? BuildEntry()
    {
        if (PokemonCombo.SelectedItem is not string name) return null;
        return new ShinyEntry
        {
            Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
            Pokemon   = name,
            Route     = _route.Name,
            Encounters= _route.Encounters,
            Resets    = _route.Resets,
            Type      = "found",
        };
    }

    private void LogContinue_Click(object sender, RoutedEventArgs e)
    {
        var entry = BuildEntry();
        if (entry is not null)
        {
            _history.Insert(0, entry);
            _route.Phases++;
            _route.RefreshStats();
        }
        DialogResult = true;
    }

    private void LogReset_Click(object sender, RoutedEventArgs e)
    {
        var entry = BuildEntry();
        if (entry is not null)
        {
            _history.Insert(0, entry);
            _route.Encounters     = 0;
            _route.Resets         = 0;
            _route.ElapsedSeconds = 0;
            foreach (var tile in _route.Tracked.Values)
                tile.Count = 0;
            _route.RefreshStats();
        }
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
        => DialogResult = false;
}
