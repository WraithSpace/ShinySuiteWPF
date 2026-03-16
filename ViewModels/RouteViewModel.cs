using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShinySuite.Models;
using ShinySuite.Services;
using System.Windows;

namespace ShinySuite.ViewModels;

public partial class RouteViewModel : ObservableObject
{
    // ── Identity ──────────────────────────────────────────────────────────────
    public string Name { get; }
    public string Game { get; set; } = "Heart Gold / Soul Silver";

    // ── Observable state ──────────────────────────────────────────────────────
    [ObservableProperty] private bool   _isExpanded;
    [ObservableProperty] private bool   _isRunning;
    [ObservableProperty] private int    _encounters;
    [ObservableProperty] private int    _resets;
    [ObservableProperty] private int    _phases;
    [ObservableProperty] private int    _elapsedSeconds;
    [ObservableProperty] private string _statsText = "";
    [ObservableProperty] private string _encText    = "0";
    [ObservableProperty] private string _ephText    = "0";
    [ObservableProperty] private string _timeText   = "00:00:00";
    [ObservableProperty] private string _probText   = "0.0%";
    [ObservableProperty] private string _expandIcon = "▼";
    [ObservableProperty] private string _startLabel = "Start";
    [ObservableProperty] private CategoryViewModel? _activeCategory;

    // ── Collections ───────────────────────────────────────────────────────────
    public ObservableCollection<CategoryViewModel> Categories { get; } = [];
    public Dictionary<string, PokemonTileViewModel> Tracked   { get; } = [];

    // ── OCR state ─────────────────────────────────────────────────────────────
    public List<OcrRegionConfig> OcrRegions { get; set; } = [];
    public Dictionary<int, RegionState> RegionState { get; } = [];
    [ObservableProperty] private string _ocrStatus = "";
    [ObservableProperty] private double _shinyOddsValue;

    // ── Events ────────────────────────────────────────────────────────────────
    public Action<RouteViewModel>? RequestShinyFound { get; set; }
    public Action<RouteViewModel>? RequestAddRegion  { get; set; }
    public Action<RouteViewModel>? RequestManageRegions { get; set; }
    public Action<RouteViewModel>? RequestRemove     { get; set; }
    public Action<RouteViewModel>? RequestPopOut    { get; set; }
    public Action?                  SaveRequested    { get; set; }

    // ── Timer ─────────────────────────────────────────────────────────────────
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };

    public RouteViewModel(string name)
    {
        Name = name;
        _timer.Tick += (_, _) => { ElapsedSeconds++; RefreshStats(); };
    }

    // ── Category management ───────────────────────────────────────────────────
    public CategoryViewModel EnsureCategory(string name)
    {
        var cat = Categories.FirstOrDefault(c => c.Name == name);
        if (cat is not null) return cat;
        cat = new CategoryViewModel(name);
        cat.ActivateRequested = () => ShowCategory(cat);
        Categories.Add(cat);
        if (ActiveCategory is null) ShowCategory(cat);
        return cat;
    }

    public void ShowCategory(CategoryViewModel cat)
    {
        if (ActiveCategory is not null) ActiveCategory.IsActive = false;
        ActiveCategory = cat;
        cat.IsActive = true;
    }

    // ── Pokemon management ────────────────────────────────────────────────────
    private string SpriteDbGame => Game switch
    {
        "Gold / Silver / Crystal"    => "gold-silver",
        "Fire Red / Leaf Green"      => "firered-leafgreen",
        "Ruby / Sapphire / Emerald"  => "emerald",
        "Diamond / Pearl / Platinum" => "platinum",
        "Black / White"              => "black-white",
        "Black 2 / White 2"          => "black-white-2",
        _                            => "heartgold-soulsilver",
    };

    public void AddPokemon(string showdownId, string displayName, string category,
        int count = 0, string? timeOfDay = null, string? version = null, bool isShinyLocked = false)
    {
        if (Tracked.ContainsKey(showdownId)) return;
        var cat  = EnsureCategory(category);
        var tile = new PokemonTileViewModel(showdownId, displayName, category, count, timeOfDay, version, SpriteDbGame, isShinyLocked);
        tile.CountChanged = delta =>
        {
            Encounters = Math.Max(0, Encounters + delta);
            RefreshStats();
            SaveRequested?.Invoke();
        };
        tile.SetAsTarget  = () => AddTarget(showdownId);
        tile.ClearTarget  = () => RemoveTarget(showdownId);
        Tracked[showdownId] = tile;
        cat.Tiles.Add(tile);
    }

    public PokemonTileViewModel? TargetTile =>
        Tracked.Values.FirstOrDefault(t => t.IsTarget);

    public IEnumerable<PokemonTileViewModel> TargetTiles =>
        Tracked.Values.Where(t => t.IsTarget);

    public void AddTarget(string sid)
    {
        if (Tracked.TryGetValue(sid, out var tile))
            tile.IsTarget = true;
        OnPropertyChanged(nameof(TargetTile));
        OnPropertyChanged(nameof(TargetTiles));
        SaveRequested?.Invoke();
    }

    public void RemoveTarget(string sid)
    {
        if (Tracked.TryGetValue(sid, out var tile))
            tile.IsTarget = false;
        OnPropertyChanged(nameof(TargetTile));
        OnPropertyChanged(nameof(TargetTiles));
        SaveRequested?.Invoke();
    }

    public void IncrementPokemon(string sid, int delta = 1)
    {
        if (!Tracked.TryGetValue(sid, out var tile)) return;
        int newCount = Math.Max(0, tile.Count + delta);
        int actual   = newCount - tile.Count;
        tile.Count   = newCount;
        Encounters   = Math.Max(0, Encounters + actual);
        RefreshStats();
        SaveRequested?.Invoke();
    }

    // ── Stats ─────────────────────────────────────────────────────────────────
    public void RefreshStats()
    {
        bool showEnc    = Categories.Any(c => EncounterDbService.RandomEncCats.Contains(c.Name));
        bool showResets = Categories.Any(c => EncounterDbService.ResetCats.Contains(c.Name));
        double prob     = Encounters > 0 ? (1 - Math.Pow(8191.0 / 8192, Encounters)) * 100 : 0;
        int eph         = ElapsedSeconds > 0 ? (int)(Encounters / (ElapsedSeconds / 3600.0)) : 0;
        var parts       = new List<string>();
        if (showEnc)    parts.Add($"Enc: {Encounters}  |  Enc/hr: {eph}  |  Phase: {Phases}");
        if (showResets) parts.Add($"Resets: {Resets}");
        parts.Add($"{FormatTime(ElapsedSeconds)}  |  Prob: {prob:F1}%");
        StatsText      = string.Join("  |  ", parts);
        EncText        = $"{Encounters:N0}";
        EphText        = $"{eph:N0}";
        TimeText       = FormatTime(ElapsedSeconds);
        ProbText       = $"{prob:F1}%";
        ShinyOddsValue = Math.Min(prob, 100);
    }

    private static string FormatTime(int seconds)
    {
        int h = seconds / 3600, m = (seconds % 3600) / 60, s = seconds % 60;
        return $"{h:D2}:{m:D2}:{s:D2}";
    }

    // ── Commands ──────────────────────────────────────────────────────────────
    [RelayCommand]
    private void ToggleExpand()
    {
        IsExpanded = !IsExpanded;
        ExpandIcon = IsExpanded ? "▲" : "▼";
    }

    [RelayCommand]
    private void ToggleRunning()
    {
        IsRunning = !IsRunning;
        StartLabel = IsRunning ? "Stop" : "Start";
        if (IsRunning) { _timer.Start(); }
        else           { _timer.Stop();  }
        SaveRequested?.Invoke();
    }

    [RelayCommand]
    private void ShinyFound()
    {
        RequestShinyFound?.Invoke(this);
    }
    [RelayCommand] private void OpenMenu()         => RequestManageRegions?.Invoke(this);
    [RelayCommand] private void Remove()           => RequestRemove?.Invoke(this);
    [RelayCommand] private void PopOut()           => RequestPopOut?.Invoke(this);

    // ── Deactivate (called when switching games) ──────────────────────────────
    public void ForceStop()
    {
        if (!IsRunning) return;
        IsRunning  = false;
        StartLabel = "Start";
        _timer.Stop();
    }

    // ── Config ────────────────────────────────────────────────────────────────
    public RouteConfig ToConfig() => new()
    {
        Name          = Name,
        Game          = Game,
        Encounters    = Encounters,
        Resets        = Resets,
        Phases        = Phases,
        ElapsedSeconds= ElapsedSeconds,
        TargetSids    = Tracked.Where(kvp => kvp.Value.IsTarget).Select(kvp => kvp.Key).ToList(),
        IsExpanded    = IsExpanded,
        OcrRegions    = OcrRegions,
        Pokemon       = Tracked.Values.Select(t => new PokemonConfig
        {
            ShowdownId  = t.ShowdownId,
            DisplayName = t.DisplayName,
            Category    = t.Category,
            Count       = t.Count,
            TimeOfDay   = t.TimeOfDay,
            Version     = t.Version,
        }).ToList(),
    };
}

public class RegionState
{
    public bool Armed  { get; set; } = true;
    public int  Stable { get; set; }
    public int  NoConf { get; set; }
}
