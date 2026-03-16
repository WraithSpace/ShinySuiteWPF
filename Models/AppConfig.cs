using System.Collections.Generic;

namespace ShinySuite.Models;

public class AppConfig
{
    public string Game   { get; set; } = "Heart Gold / Soul Silver";
    public List<RouteConfig>  Routes         { get; set; } = [];
    public List<RouteConfig>  ArchivedRoutes { get; set; } = [];
    public List<ShinyEntry>   ShinyHistory   { get; set; } = [];
    public double  UiScale        { get; set; } = 1.0;
    public double  WindowWidth    { get; set; } = 1280;
    public double  WindowHeight   { get; set; } = 860;
    public double? WindowLeft     { get; set; }
    public double? WindowTop      { get; set; }
    public bool    WindowMaximized          { get; set; }
    public bool    SkipRemoveRouteConfirm   { get; set; }
    public bool    SidebarExpanded         { get; set; } = true;
}

public class RouteConfig
{
    public string  Name          { get; set; } = "";
    public string  Game          { get; set; } = "Heart Gold / Soul Silver";
    public int     Encounters    { get; set; }
    public int     Resets        { get; set; }
    public int     Phases        { get; set; }
    public int     ElapsedSeconds{ get; set; }
    public List<string> TargetSids { get; set; } = [];
    public bool    IsExpanded    { get; set; }
    public List<OcrRegionConfig> OcrRegions { get; set; } = [];
    public List<PokemonConfig>   Pokemon    { get; set; } = [];
}

public class OcrRegionConfig
{
    public int X { get; set; }
    public int Y { get; set; }
    public int W { get; set; }
    public int H { get; set; }
}

public class PokemonConfig
{
    public string  ShowdownId  { get; set; } = "";
    public string  DisplayName { get; set; } = "";
    public string  Category    { get; set; } = "Other";
    public int     Count       { get; set; }
    public string? TimeOfDay   { get; set; }
    public string? Version     { get; set; }
}

public class ShinyEntry
{
    public string Timestamp { get; set; } = "";
    public string Pokemon   { get; set; } = "";
    public string Route     { get; set; } = "";
    public int    Encounters { get; set; }
    public int    Resets     { get; set; }
    public string Type      { get; set; } = "found";
}
