using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ShinySuite.ViewModels;

public partial class PokemonTileViewModel : ObservableObject
{
    public string  ShowdownId    { get; }
    public string  DisplayName   { get; }
    public string  Category      { get; }
    public string? TimeOfDay     { get; }   // "day", "night", null
    public string? Version       { get; private set; }   // "hg", "ss", null
    public bool    IsShinyLocked { get; }

    [ObservableProperty] private BitmapImage? _sprite;
    [ObservableProperty] private int          _count;
    [ObservableProperty] private bool         _isTarget;

    private BitmapImage? _normalSprite;
    private readonly string _spriteDbGame;  // pokemondb game slug
    private static readonly HttpClient _http = new();

    // When a sprite 404s in the primary slug, try these in order (same art generation).
    private static readonly Dictionary<string, string[]> _fallbacks = new()
    {
        ["firered-leafgreen"]    = ["ruby-sapphire", "emerald"],
        ["heartgold-soulsilver"] = ["platinum", "diamond-pearl"],
        ["emerald"]              = ["ruby-sapphire"],
        ["black-white"]          = ["black-2-white-2"],
        ["black-white-2"]        = ["black-white"],
        ["platinum"]             = ["diamond-pearl"],
    };

    // Callbacks set by RouteViewModel
    public Action<int>? CountChanged { get; set; }
    public Action?      SetAsTarget  { get; set; }
    public Action?      ClearTarget  { get; set; }

    public void SetVersion(string? v)
    {
        if (Version == v) return;
        Version = v;
        OnPropertyChanged(nameof(Version));
        OnPropertyChanged(nameof(VersionText));
        OnPropertyChanged(nameof(VersionVisibility));
        OnPropertyChanged(nameof(VersionColor));
        OnPropertyChanged(nameof(Version2Text));
        OnPropertyChanged(nameof(Version2Visibility));
        OnPropertyChanged(nameof(Version2Color));
        OnPropertyChanged(nameof(VersionIcon));
        OnPropertyChanged(nameof(Version2Icon));
    }

    // Derived display
    public bool         ShowTimeIcon  => TimeOfDay is "day" or "night";
    public BitmapImage? TimeIconImage => GetVersionIcon(TimeOfDay ?? "");
    public string       TimeLabel     => TimeOfDay == "day" ? "Day" : "Night";

    public string VersionLabel => PrimaryVersion switch
    {
        "g"  => "Gold",        "si" => "Silver",      "c"  => "Crystal",
        "hg" => "Heart Gold",  "ss" => "Soul Silver",
        "fr" => "Fire Red",    "lg" => "Leaf Green",
        "r"  => "Ruby",
        "s"  => "Sapphire",
        "e"  => "Emerald",
        "b"  => "Black",
        "w"  => "White",
        "b2" => "Black 2",
        "w2" => "White 2",
        "d"  => "Diamond",
        "p"  => "Pearl",
        "pl" => "Platinum",
        _    => "",
    };
    public string Version2Label => "Emerald";

    // Dual-badge helpers: "er"/"es" → left E badge + right R/S badge
    private bool    IsDual         => Version is "er" or "es";
    private string? PrimaryVersion => IsDual ? Version![1..] : Version;

    public string     VersionText       => PrimaryVersion?.ToUpper() ?? "";
    public Visibility VersionVisibility => PrimaryVersion is "g" or "si" or "c" or "r" or "s" or "hg" or "ss" or "fr" or "lg" or "e" or "b" or "w" or "b2" or "w2" or "d" or "p" or "pl"
                                           ? Visibility.Visible : Visibility.Collapsed;
    public string VersionColor => PrimaryVersion switch
    {
        "g"         => "#DAA520",
        "si"        => "#A0A0B0",
        "c"         => "#44AACC",
        "hg"        => "#E6B800",
        "fr" or "r" => "#CC2200",
        "lg"        => "#007733",
        "s"         => "#0044CC",
        "e"         => "#44AA44",
        "b"         => "#6688CC",
        "w"         => "#DDBBAA",
        "b2"        => "#446699",
        "w2"        => "#CCAACC",
        "d"         => "#4444AA",
        "p"         => "#AAAACC",
        "pl"        => "#886699",
        _           => "#C0C8E0",
    };

    // Left E badge (visible only for dual "er"/"es")
    public string     Version2Text       => IsDual ? "E" : "";
    public Visibility Version2Visibility => IsDual ? Visibility.Visible : Visibility.Collapsed;
    public string     Version2Color      => "#44AA44";

    // Shuffle icons
    public BitmapImage? VersionIcon  => GetVersionIcon(PrimaryVersion);
    public BitmapImage? Version2Icon => IsDual ? GetVersionIcon("e") : null;

    private static readonly Dictionary<string, BitmapImage?> _iconCache = new();

    private static BitmapImage? GetVersionIcon(string? ver)
    {
        if (ver is null) return null;
        if (_iconCache.TryGetValue(ver, out var cached)) return cached;
        var path = FindVersionIconPath(ver);
        if (path is null) { _iconCache[ver] = null; return null; }
        try
        {
            var img = new BitmapImage();
            img.BeginInit();
            img.UriSource   = new Uri(path, UriKind.Absolute);
            img.CacheOption = BitmapCacheOption.OnLoad;
            img.EndInit();
            img.Freeze();
            _iconCache[ver] = img;
            return img;
        }
        catch { _iconCache[ver] = null; return null; }
    }

    private static string? FindVersionIconPath(string ver)
    {
        var exeDir = AppContext.BaseDirectory;
        var p = Path.Combine(exeDir, "sprites", "version-icons", $"{ver}.png");
        return File.Exists(p) ? p : null;
    }

    public string     TimeColor          => TimeOfDay == "day" ? "#E6B800" : "#88AAFF";
    public Visibility ShinyLockVisibility => IsShinyLocked ? Visibility.Visible : Visibility.Collapsed;

    public PokemonTileViewModel(string showdownId, string displayName, string category,
        int count = 0, string? timeOfDay = null, string? version = null,
        string spriteDbGame = "heartgold-soulsilver", bool isShinyLocked = false)
    {
        ShowdownId    = showdownId;
        DisplayName   = displayName;
        Category      = category;
        _count        = count;
        TimeOfDay     = timeOfDay;
        Version       = version;
        IsShinyLocked = isShinyLocked;
        _spriteDbGame = spriteDbGame;

        // Try local cache synchronously first; async-fetch from pokemondb if not found
        var local = LoadLocalSprite(showdownId, spriteDbGame, "normal");
        if (local is not null)
            _sprite = _normalSprite = local;
        else
            _ = LoadNormalSpriteAsync();
    }

    // ── Target toggle: swap sprite ─────────────────────────────────────────────

    partial void OnIsTargetChanged(bool value)
    {
        if (!value)
            Sprite = _normalSprite;
        else
            _ = LoadShinyAsync();
    }

    // ── Async sprite loaders ───────────────────────────────────────────────────

    private async Task LoadNormalSpriteAsync()
    {
        var img = await FetchSprite("normal");
        if (img is not null)
        {
            _normalSprite = img;
            if (!IsTarget) Sprite = img;
        }
    }

    private async Task LoadShinyAsync()
    {
        // Try local shiny cache first
        var local = LoadLocalSprite(ShowdownId, _spriteDbGame, "shiny");
        if (local is not null) { Sprite = local; return; }

        var img = await FetchSprite("shiny");
        if (img is not null)
            Sprite = img;
        // else keep normal sprite
    }

    // Fetch from pokemondb, with fallback slugs for cross-gen Pokémon
    private async Task<BitmapImage?> FetchSprite(string type)
    {
        var ids = ShowdownId.Contains('-')
            ? new[] { ShowdownId, ShowdownId.Replace("-", "") }
            : new[] { ShowdownId };

        // pokemondb gender variant naming: frillish-male → frillish, frillish-female → frillish-f
        if (ShowdownId.EndsWith("-male"))
            ids = [.. ids, ShowdownId[..^5]];
        else if (ShowdownId.EndsWith("-female"))
            ids = [.. ids, ShowdownId[..^7] + "-f", ShowdownId[..^7]];

        var slugs = new[] { _spriteDbGame }
            .Concat(_fallbacks.GetValueOrDefault(_spriteDbGame) ?? []);

        foreach (var slug in slugs)
        foreach (var id in ids)
        {
            try
            {
                // Prefer animated GIF; fall back to static PNG
                var url = $"https://img.pokemondb.net/sprites/{slug}/anim/{type}/{id}.gif";
                byte[]? bytes = null;
                try { bytes = await _http.GetByteArrayAsync(url); } catch { }
                if (bytes is null)
                {
                    url   = $"https://img.pokemondb.net/sprites/{slug}/{type}/{id}.png";
                    bytes = await _http.GetByteArrayAsync(url);
                }
                var img   = new BitmapImage();
                img.BeginInit();
                img.StreamSource = new MemoryStream(bytes);
                img.CacheOption  = BitmapCacheOption.OnLoad;
                img.EndInit();
                img.Freeze();
                return img;
            }
            catch { }
        }
        return null;
    }

    // ── Local sprite loading ───────────────────────────────────────────────────

    private static BitmapImage? LoadLocalSprite(string sid, string gameSlug, string type)
    {
        var path = FindSpritePath(sid, gameSlug, type);
        if (path is null) return null;
        try
        {
            var img = new BitmapImage();
            img.BeginInit();
            img.UriSource     = new Uri(path, UriKind.Absolute);
            img.CacheOption   = BitmapCacheOption.OnLoad;
            img.CreateOptions = BitmapCreateOptions.None;
            img.EndInit();
            img.Freeze();
            return img;
        }
        catch { return null; }
    }

    private static string? FindSpritePath(string sid, string gameSlug, string type)
    {
        var exeDir = AppContext.BaseDirectory;
        var ids = new List<string> { sid, sid.Replace("-", "") };
        // Gender variants: frillish-male → also try "frillish"; frillish-female → "frillish-f", "frillish"
        if (sid.EndsWith("-male"))
            ids.Add(sid[..^5]);
        else if (sid.EndsWith("-female"))
            ids.AddRange(new[] { sid[..^7] + "-f", sid[..^7] });
        foreach (var id in ids)
        foreach (var ext in new[] { ".gif", ".png" })   // animated preferred
        {
            var p = Path.Combine(exeDir, "sprites", gameSlug, type, id + ext);
            if (File.Exists(p)) return p;
        }
        return null;
    }

    // ── Commands ───────────────────────────────────────────────────────────────

    [RelayCommand]
    private void Increment()
    {
        Count++;
        CountChanged?.Invoke(1);
    }

    [RelayCommand]
    private void Decrement()
    {
        if (Count <= 0) return;
        Count--;
        CountChanged?.Invoke(-1);
    }

    public void SetCount(int newValue)
    {
        int clamped = Math.Max(0, newValue);
        int delta   = clamped - Count;
        if (delta == 0) return;
        Count = clamped;
        CountChanged?.Invoke(delta);
    }

    [RelayCommand] private void SetTarget()    => SetAsTarget?.Invoke();
    [RelayCommand] private void RemoveTarget() => ClearTarget?.Invoke();
}
