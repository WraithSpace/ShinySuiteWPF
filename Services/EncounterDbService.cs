using System.Collections.Generic;
using System.IO;
using System.Text.Json.Nodes;
using ShinySuite.Models;

namespace ShinySuite.Services;

public record LocationEncounters(
    List<string> Walking,
    List<string> Surfing,
    List<string> Fishing,
    List<string> Headbutt,
    List<string> Radio,
    List<string> DoubleGrass,
    List<string> ShakingSpots,
    List<string> Interactable,
    List<string> Gift,
    List<string> Roaming,
    List<string> HiddenGrotto,
    List<string> RockSmash,
    List<string> Other,
    Dictionary<string, string> WalkingTime,
    Dictionary<string, string> Versions,
    List<string> ShinyLocked
);

public static class EncounterDbService
{
    public static readonly string[] CategoryOrder =
        ["Walking", "Double Grass", "Surfing", "Fishing", "Headbutt", "Radio",
         "Shaking Spots", "Interactable", "Gift", "Roaming", "Hidden Grotto", "Rock Smash", "Other"];

    public static readonly HashSet<string> RandomEncCats =
        ["Walking", "Double Grass", "Surfing", "Fishing", "Headbutt", "Radio",
         "Shaking Spots", "Rock Smash"];

    public static readonly HashSet<string> ResetCats =
        ["Interactable", "Gift", "Roaming", "Hidden Grotto"];

    public static Dictionary<string, Dictionary<string, LocationEncounters>> Load(string path)
    {
        if (!File.Exists(path)) return [];
        try
        {
            var root   = JsonNode.Parse(File.ReadAllText(path))?.AsObject() ?? new JsonObject();
            var result = new Dictionary<string, Dictionary<string, LocationEncounters>>(StringComparer.OrdinalIgnoreCase);
            foreach (var (gameName, gameNode) in root)
            {
                if (gameNode is not JsonObject gameObj) continue;
                var locs = new Dictionary<string, LocationEncounters>(StringComparer.OrdinalIgnoreCase);
                foreach (var (locName, locNode) in gameObj)
                {
                    if (locNode is not JsonObject o) continue;
                    locs[locName] = new LocationEncounters(
                        GetList(o, "Walking"), GetList(o, "Surfing"), GetList(o, "Fishing"),
                        GetList(o, "Headbutt"), GetList(o, "Radio"),
                        GetList(o, "Double Grass"), GetList(o, "Shaking Spots"),
                        GetList(o, "Interactable"), GetList(o, "Gift"), GetList(o, "Roaming"),
                        GetList(o, "Hidden Grotto"), GetList(o, "Rock Smash"), GetList(o, "Other"),
                        GetDict(o, "WalkingTime"), GetDict(o, "Versions"),
                        GetList(o, "ShinyLocked"));
                }
                result[gameName] = locs;
            }
            return result;
        }
        catch
        {
            return [];
        }
    }

    public static List<string> GetPokemon(LocationEncounters loc, string category) => category switch
    {
        "Walking"       => loc.Walking,
        "Surfing"       => loc.Surfing,
        "Fishing"       => loc.Fishing,
        "Headbutt"      => loc.Headbutt,
        "Radio"         => loc.Radio,
        "Double Grass"  => loc.DoubleGrass,
        "Shaking Spots" => loc.ShakingSpots,
        "Interactable"  => loc.Interactable,
        "Gift"          => loc.Gift,
        "Roaming"        => loc.Roaming,
        "Hidden Grotto"  => loc.HiddenGrotto,
        "Rock Smash"     => loc.RockSmash,
        _               => loc.Other,
    };

    private static List<string> GetList(JsonObject o, string key)
    {
        var list = new List<string>();
        if (o[key] is JsonArray arr)
            foreach (var item in arr)
                if (item?.GetValue<string>() is string s) list.Add(s);
        return list;
    }

    private static Dictionary<string, string> GetDict(JsonObject o, string key)
    {
        var dict = new Dictionary<string, string>();
        if (o[key] is JsonObject sub)
            foreach (var (k, v) in sub)
                dict[k] = v?.GetValue<string>() ?? "";
        return dict;
    }
}
