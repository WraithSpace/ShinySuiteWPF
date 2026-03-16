using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShinySuite.Data;
using ShinySuite.Models;
using ShinySuite.Services;

namespace ShinySuite.ViewModels;

public partial class MainViewModel : ObservableObject
{
    // ── State ─────────────────────────────────────────────────────────────────
    [ObservableProperty] private string _selectedGame        = "Heart Gold / Soul Silver";
    [ObservableProperty] private string _globalStats         = "Enc: 0  |  Enc/hr: 0  |  Phase: 0  |  Resets: 0  |  00:00:00";
    [ObservableProperty] private string _searchText          = "";
    [ObservableProperty] private double _uiScale             = 1.0;
    [ObservableProperty] private string _selectedScale       = "100%";

    partial void OnSelectedScaleChanged(string value)
    {
        UiScale = value switch { "75%" => 0.75, "125%" => 1.25, "150%" => 1.5, _ => 1.0 };
        _config.UiScale = UiScale;
        if (!_loading) SaveConfig();
    }

    public List<string> ScaleOptions { get; } = ["75%", "100%", "125%", "150%"];

    private const string Accent = "#E6B800";

    public ObservableCollection<RouteViewModel> Routes { get; } = [];
    public List<string> GameList => [.. GameData.GameLocations.Keys];
    public List<string> Filtered => FilteredLocations();

    public RouteViewModel? ActiveHuntRoute => Routes.FirstOrDefault(r => r.IsRunning);
    public event Action<RouteViewModel>? PopOutRequested;

    // ── Data ──────────────────────────────────────────────────────────────────
    private readonly Dictionary<string, Dictionary<string, LocationEncounters>> _db;
    private readonly AppConfig _config;
    public AppConfig Config => _config;
    private readonly DispatcherTimer _globalTimer    = new() { Interval = TimeSpan.FromSeconds(1) };
    private readonly DispatcherTimer _autosaveTimer  = new() { Interval = TimeSpan.FromSeconds(30) };
    private CancellationTokenSource? _detectionCts;
    private readonly DetectionService _detection = new();
    private bool _loading;
    // Tracks the game whose routes are currently in the Routes collection.
    // Needed because SelectedGame changes before OnSelectedGameChanged saves.
    private string _activeGame = "Heart Gold / Soul Silver";

    public MainViewModel()
    {
        var dbPath = FindDataFile("route_encounters.json");
        _db        = EncounterDbService.Load(dbPath);
        _config    = ConfigService.Load();
        ApplyAccent();
        _globalTimer.Tick   += (_, _) => RefreshGlobalStats();
        _autosaveTimer.Tick += (_, _) => SaveConfig();
        _globalTimer.Start();
        _autosaveTimer.Start();
        LoadConfig();
        StartDetection();
    }

    private static void ApplyAccent()
    {
        try
        {
            var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(Accent);
            Application.Current.Resources["AccentColor"] = new System.Windows.Media.SolidColorBrush(color);
        }
        catch { }
    }

    // ── Location search ───────────────────────────────────────────────────────
    partial void OnSearchTextChanged(string value) => OnPropertyChanged(nameof(Filtered));
    partial void OnSelectedGameChanged(string value)
    {
        OnPropertyChanged(nameof(Filtered));
        if (_loading) return;
        // Save current game's routes before switching (_activeGame still points to old game)
        SaveConfig();
        _activeGame = value;
        foreach (var r in Routes) r.ForceStop();
        Routes.Clear();
        LoadRoutesForGame(value);
    }

    private List<string> FilteredLocations()
    {
        var allLocations = GameData.GameLocations.GetValueOrDefault(SelectedGame) ?? [];
        var existing     = Routes.Select(r => r.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var q            = SearchText.Trim();
        return allLocations
            .Where(loc => !existing.Contains(loc)
                && (q.Length == 0 || LocationMatchesQuery(loc, q)))
            .ToList();
    }

    private Dictionary<string, LocationEncounters> DbForGame(string game)
        => _db.GetValueOrDefault(game) ?? [];

    private bool LocationMatchesQuery(string loc, string q)
    {
        if (loc.Contains(q, StringComparison.OrdinalIgnoreCase)) return true;
        if (!DbForGame(SelectedGame).TryGetValue(loc, out var locData)) return false;
        foreach (var cat in EncounterDbService.CategoryOrder)
        {
            var pokes = EncounterDbService.GetPokemon(locData, cat);
            if (!pokes.Any()) continue;
            if (cat.Contains(q, StringComparison.OrdinalIgnoreCase)) return true;
            if (pokes.Any(p =>
                    p.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    ToDisplayName(p).Contains(q, StringComparison.OrdinalIgnoreCase)))
                return true;
        }
        return false;
    }

    // ── Routes ────────────────────────────────────────────────────────────────
    [RelayCommand]
    public void AddRoute(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return;
        if (Routes.Any(r => r.Name.Equals(name, StringComparison.OrdinalIgnoreCase))) return;

        var route    = CreateRoute(name, SelectedGame);
        var archived = _config.ArchivedRoutes
            .FirstOrDefault(r => r.Name.Equals(name, StringComparison.OrdinalIgnoreCase)
                              && r.Game == SelectedGame);

        if (archived is not null)
        {
            // Restore previously-removed route data
            route.Encounters     = archived.Encounters;
            route.Resets         = archived.Resets;
            route.Phases         = archived.Phases;
            route.ElapsedSeconds = archived.ElapsedSeconds;
            route.OcrRegions     = archived.OcrRegions;
            var dbLocA = DbForGame(route.Game).GetValueOrDefault(route.Name);
            foreach (var pc in archived.Pokemon)
                route.AddPokemon(pc.ShowdownId, pc.DisplayName, pc.Category,
                    pc.Count, pc.TimeOfDay, pc.Version,
                    dbLocA?.ShinyLocked.Contains(pc.ShowdownId) ?? false);
            foreach (var sid in archived.TargetSids)
                route.AddTarget(sid);
            SyncNewPokemonFromDb(route);  // add any newly-added DB Pokémon
        }
        else
        {
            AutoPopulate(route);
        }

        route.RefreshStats();
        if (!route.IsExpanded) route.ToggleExpandCommand.Execute(null);
        Routes.Insert(0, route);
        OnPropertyChanged(nameof(Filtered));
        SearchText = "";
        SaveConfig();
    }

    public void RemoveRoute(RouteViewModel route)
    {
        if (!_config.SkipRemoveRouteConfirm)
        {
            var dlg = new Views.ConfirmDialog(
                $"Remove '{route.Name}'?",
                "This location will be removed from the dashboard.\n\nYour data is preserved — re-adding it will restore all counts.",
                showDontAskAgain: true)
                { Owner = MainWin };
            if (dlg.ShowDialog() != true) return;
            if (dlg.DontAskAgain)
            {
                _config.SkipRemoveRouteConfirm = true;
                SaveConfig();
            }
        }

        route.RequestRemove = null;
        // Archive data before removing so re-adding restores counts
        _config.ArchivedRoutes.RemoveAll(r =>
            r.Name.Equals(route.Name, StringComparison.OrdinalIgnoreCase) &&
            r.Game == route.Game);
        _config.ArchivedRoutes.Add(route.ToConfig());
        Routes.Remove(route);
        OnPropertyChanged(nameof(Filtered));
        SaveConfig();
    }

    public void PermanentlyDeleteRoute(RouteViewModel route)
    {
        route.RequestRemove = null;
        _config.ArchivedRoutes.RemoveAll(r =>
            r.Name.Equals(route.Name, StringComparison.OrdinalIgnoreCase) &&
            r.Game == route.Game);
        Routes.Remove(route);
        OnPropertyChanged(nameof(Filtered));
        SaveConfig();
    }

    private RouteViewModel CreateRoute(string name, string game)
    {
        var route = new RouteViewModel(name) { Game = game };
        route.SaveRequested        = SaveConfig;
        route.RequestShinyFound    = ShowShinyDialog;
        route.RequestRemove        = RemoveRoute;
        route.RequestPopOut        = r => PopOutRequested?.Invoke(r);
        // Move route to top of list when it starts running; notify mini player
        route.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(RouteViewModel.IsRunning))
            {
                if (route.IsRunning)
                {
                    int idx = Routes.IndexOf(route);
                    if (idx > 0) Routes.Move(idx, 0);
                }
                OnPropertyChanged(nameof(ActiveHuntRoute));
            }
        };
        return route;
    }

    private void AutoPopulate(RouteViewModel route)
    {
        var gameDb = DbForGame(route.Game);
        if (!gameDb.TryGetValue(route.Name, out var loc)) return;
        foreach (var cat in EncounterDbService.CategoryOrder)
        {
            var list = EncounterDbService.GetPokemon(loc!, cat);
            foreach (var sid in list)
            {
                string? tod     = cat == "Walking" && loc!.WalkingTime.TryGetValue(sid, out var t) ? t : null;
                string? ver     = loc!.Versions.TryGetValue(sid, out var v) ? v : null;
                bool    locked  = loc!.ShinyLocked.Contains(sid);
                var display = ToDisplayName(sid);
                route.AddPokemon(sid, display, cat, 0, tod, ver, locked);
            }
        }
        route.RefreshStats();
    }

    // Adds any DB Pokémon not yet tracked (preserves existing counts); patches version on existing tiles
    private void SyncNewPokemonFromDb(RouteViewModel route)
    {
        if (!DbForGame(route.Game).TryGetValue(route.Name, out var loc)) return;
        foreach (var cat in EncounterDbService.CategoryOrder)
        {
            var list = EncounterDbService.GetPokemon(loc, cat);
            foreach (var sid in list)
            {
                string? tod    = cat == "Walking" && loc.WalkingTime.TryGetValue(sid, out var t) ? t : null;
                string? ver    = loc.Versions.TryGetValue(sid, out var v) ? v : null;
                bool    locked = loc.ShinyLocked.Contains(sid);
                if (route.Tracked.TryGetValue(sid, out var existing))
                    existing.SetVersion(ver);
                else
                    route.AddPokemon(sid, ToDisplayName(sid), cat, 0, tod, ver, locked);
            }
        }
    }

    private static Window MainWin => Application.Current.MainWindow;

    // ── Manage locations dialog ───────────────────────────────────────────────
    [RelayCommand]
    private void ManageLocations()
    {
        Action? resetPrompts = _config.SkipRemoveRouteConfirm
            ? () => { _config.SkipRemoveRouteConfirm = false; SaveConfig(); }
            : null;
        var dlg = new Views.ManageLocationsDialog(Routes.ToList(), PermanentlyDeleteRoute, resetPrompts)
            { Owner = MainWin };
        dlg.ShowDialog();
    }

    // ── Shiny found dialog ────────────────────────────────────────────────────
    private void ShowShinyDialog(RouteViewModel route)
    {
        var dlg = new Views.ShinyFoundDialog(route, _config.ShinyHistory, Accent)
            { Owner = MainWin };
        dlg.ShowDialog();
        SaveConfig();
    }

    // ── Capture / Manage regions (delegated to active route) ──────────────────
    [RelayCommand]
    private void CaptureText()
    {
        var route = GetActiveRoute();
        if (route is null) { MessageBox.Show(MainWin, "Add a location first.", "ShinySuite"); return; }
        var overlay = new Views.ScreenSelectOverlay();
        if (overlay.ShowDialog() == true)
        {
            var r = overlay.SelectedRegion;
            route.OcrRegions.Add(new OcrRegionConfig { X = r.X, Y = r.Y, W = r.Width, H = r.Height });
            SaveConfig();
        }
    }

    [RelayCommand]
    private void ManageCaptures()
    {
        var route = GetActiveRoute();
        if (route is null) { MessageBox.Show(MainWin, "Add a location first.", "ShinySuite"); return; }
        var dlg = new Views.ManageRegionsDialog(route) { Owner = MainWin };
        dlg.ShowDialog();
        SaveConfig();
    }

    [RelayCommand]
    private void ShowHistory()
    {
        var dlg = new Views.HistoryDialog(_config.ShinyHistory, SaveConfig) { Owner = MainWin };
        dlg.ShowDialog();
    }

    private RouteViewModel? GetActiveRoute()
        => Routes.FirstOrDefault(r => r.IsRunning) ?? Routes.FirstOrDefault();

    // ── Global stats ──────────────────────────────────────────────────────────
    private void RefreshGlobalStats()
    {
        int totalEnc    = Routes.Sum(r => r.Encounters);
        int totalSec    = Routes.Sum(r => r.ElapsedSeconds);
        int totalPhases = Routes.Sum(r => r.Phases);
        int totalResets = Routes.Sum(r => r.Resets);
        int eph         = totalSec > 0 ? (int)(totalEnc / (totalSec / 3600.0)) : 0;
        GlobalStats = $"Enc: {totalEnc}  |  Enc/hr: {eph}  |  Phase: {totalPhases}  |  Resets: {totalResets}  |  {FormatTime(totalSec)}";
    }

    private static string FormatTime(int s) => $"{s/3600:D2}:{(s%3600)/60:D2}:{s%60:D2}";

    // ── Detection loop ────────────────────────────────────────────────────────
    public void StartDetection()
    {
        _detectionCts?.Cancel();
        _detectionCts = new CancellationTokenSource();
        var token = _detectionCts.Token;
        _ = Task.Run(async () => await DetectionLoop(token), token);
    }

    public void StopDetection()
    {
        _detectionCts?.Cancel();
    }

    private const double CountCooldown = 2.0;

    private async Task DetectionLoop(CancellationToken token)
    {
        var lastCounted = new Dictionary<int, Dictionary<string, double>>();
        while (!token.IsCancellationRequested)
        {
            try
            {
                foreach (var route in Routes.ToList())
                {
                    if (!route.IsRunning || route.OcrRegions.Count == 0) continue;
                    var candidates = route.Tracked
                        .Select(kvp => (kvp.Key, kvp.Value.DisplayName))
                        .ToList();

                    string statusText = "";
                    for (int idx = 0; idx < route.OcrRegions.Count; idx++)
                    {
                        var reg = route.OcrRegions[idx];
                        using var bmp = DetectionService.CaptureRegion(reg.X, reg.Y, reg.W, reg.H);
                        var text = await _detection.OcrAsync(bmp);
                        var sid  = DetectionService.MatchPokemon(text, candidates);

                        statusText = string.IsNullOrWhiteSpace(text)
                            ? "[no text]"
                            : sid is not null ? $"✓ {sid}" : $"? {text}";

                        if (!route.RegionState.TryGetValue(idx, out var st))
                        {
                            st = new RegionState();
                            route.RegionState[idx] = st;
                        }

                        if (sid is null)
                        {
                            st.Stable = 0;
                            if (++st.NoConf >= 4) { st.Armed = true; st.NoConf = 0; }
                        }
                        else
                        {
                            st.NoConf = 0;
                            if (st.Armed && ++st.Stable >= 1)
                            {
                                var rlc = lastCounted.GetValueOrDefault(idx) ?? [];
                                lastCounted[idx] = rlc;
                                double now = (DateTime.UtcNow - DateTime.UnixEpoch).TotalSeconds;
                                if (now >= rlc.GetValueOrDefault(sid) + CountCooldown)
                                {
                                    rlc[sid] = now;
                                    st.Armed  = false;
                                    st.Stable = 0;
                                    var capturedSid   = sid;
                                    var capturedRoute = route;
                                    _ = Application.Current.Dispatcher.InvokeAsync(() =>
                                        capturedRoute.IncrementPokemon(capturedSid));
                                }
                            }
                        }
                    }
                    var capturedStatus = statusText;
                    var capturedRouteForStatus = route;
                    _ = Application.Current.Dispatcher.InvokeAsync(() =>
                        capturedRouteForStatus.OcrStatus = capturedStatus);
                }
                await Task.Delay(300, token);
            }
            catch (OperationCanceledException) { break; }
            catch { await Task.Delay(500, token); }
        }
    }

    // ── Config ────────────────────────────────────────────────────────────────
    private void LoadConfig()
    {
        _loading = true;
        SelectedGame  = _config.Game;
        _activeGame   = _config.Game;
        UiScale       = _config.UiScale > 0 ? _config.UiScale : 1.0;
        SelectedScale = UiScale switch { 0.75 => "75%", 1.25 => "125%", 1.5 => "150%", _ => "100%" };
        LoadRoutesForGame(SelectedGame);
        _loading = false;
        OnPropertyChanged(nameof(Filtered));
    }

    private void LoadRoutesForGame(string game)
    {
        foreach (var rc in _config.Routes.Where(r => r.Game == game))
        {
            var route = CreateRoute(rc.Name, game);
            route.SaveRequested  = null;  // Prevent premature saves while loading
            route.Encounters     = rc.Encounters;
            route.Resets         = rc.Resets;
            route.Phases         = rc.Phases;
            route.ElapsedSeconds = rc.ElapsedSeconds;
            route.OcrRegions     = rc.OcrRegions;

            var dbLoc = DbForGame(game).GetValueOrDefault(rc.Name);
            foreach (var pc in rc.Pokemon)
                route.AddPokemon(pc.ShowdownId, pc.DisplayName, pc.Category,
                    pc.Count, pc.TimeOfDay, pc.Version,
                    dbLoc?.ShinyLocked.Contains(pc.ShowdownId) ?? false);

            if (route.Tracked.Count == 0)
                AutoPopulate(route);
            else
                SyncNewPokemonFromDb(route);

            foreach (var sid in rc.TargetSids)
                route.AddTarget(sid);
            route.RefreshStats();
            if (rc.IsExpanded) route.ToggleExpandCommand.Execute(null);
            Routes.Add(route);
            route.SaveRequested = SaveConfig;
        }
        OnPropertyChanged(nameof(Filtered));
    }

    public void SaveConfig()
    {
        _config.Game = SelectedGame;
        // Preserve routes from other games; replace _activeGame's routes.
        // (_activeGame may differ from SelectedGame during a game switch.)
        var otherGames  = _config.Routes.Where(r => r.Game != _activeGame).ToList();
        var activeRoutes = Routes.Select(r => r.ToConfig()).ToList();
        _config.Routes  = [..otherGames, ..activeRoutes];
        ConfigService.Save(_config);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private static string ToDisplayName(string sid) => sid switch
    {
        "nidoran-f" => "Nidoran ♀",
        "nidoran-m" => "Nidoran ♂",
        _ when sid.EndsWith("-male")   => ToDisplayName(sid[..^5]),
        _ when sid.EndsWith("-female") => ToDisplayName(sid[..^7]),
        _ => System.Globalization.CultureInfo.CurrentCulture.TextInfo
                 .ToTitleCase(sid.Replace("-", " ")),
    };

    private static string FindDataFile(string filename)
    {
        // Check next to exe, then current dir
        var exeDir = AppContext.BaseDirectory;
        var candidates = new[] { Path.Combine(exeDir, filename), filename };
        return candidates.FirstOrDefault(File.Exists) ?? filename;
    }
}
