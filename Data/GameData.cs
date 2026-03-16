using System.Collections.Generic;

namespace ShinySuite.Data;

public static class GameData
{
    public static readonly Dictionary<string, List<string>> GameLocations = new()
    {
        ["Gold / Silver / Crystal"] =
        [
            // Johto routes
            "Route 29","Route 30","Route 31","Route 32","Route 33",
            "Route 34","Route 35","Route 36","Route 37","Route 38",
            "Route 39","Route 40","Route 41","Route 42","Route 43",
            "Route 44","Route 45","Route 46",
            // Johto cities & towns
            "New Bark Town","Cherrygrove City","Violet City","Azalea Town",
            "Goldenrod City","Ecruteak City","Olivine City","Cianwood City",
            "Lake of Rage","Blackthorn City",
            // Johto dungeons & landmarks
            "Sprout Tower","Union Cave","Slowpoke Well","Ilex Forest",
            "National Park","Burned Tower","Bell Tower","Whirl Islands",
            "Mt. Mortar","Ice Path","Dragon's Den","Ruins of Alph",
            "Dark Cave","Tohjo Falls",
            // Kanto connector routes
            "Route 26","Route 27","Route 28",
            "Mt. Silver",
            // Kanto routes
            "Route 1","Route 2","Route 3","Route 4","Route 5",
            "Route 6","Route 7","Route 8","Route 9","Route 10",
            "Route 11","Route 12","Route 13","Route 14","Route 15",
            "Route 16","Route 17","Route 18","Route 19","Route 20",
            "Route 21","Route 22","Route 24","Route 25",
            // Kanto cities & landmarks
            "Pallet Town","Cerulean City","Vermilion City","Celadon City",
            "Fuchsia City","Cinnabar Island","Viridian City",
            "Diglett's Cave","Mt. Moon","Rock Tunnel","Power Plant",
            "Cerulean Cave","Seafoam Islands","Victory Road",
            // Special
            "Routes (GSC Roaming)",
        ],

        ["Ruby / Sapphire / Emerald"] =
        [
            // Hoenn routes
            "Route 101","Route 102","Route 103","Route 104","Route 105",
            "Route 106","Route 107","Route 108","Route 109","Route 110",
            "Route 111","Route 112","Route 113","Route 114","Route 115",
            "Route 116","Route 117","Route 118","Route 119","Route 120",
            "Route 121","Route 122","Route 123","Route 124","Route 125",
            "Route 126","Route 127","Route 128","Route 129","Route 130",
            "Route 131","Route 132","Route 133","Route 134",
            // Towns & cities
            "Littleroot Town","Oldale Town","Petalburg City","Rustboro City",
            "Dewford Town","Slateport City","Mauville City","Verdanturf Town",
            "Fallarbor Town","Lavaridge Town","Fortree City","Lilycove City",
            "Mossdeep City","Sootopolis City","Pacifidlog Town","Ever Grande City",
            // Dungeons & landmarks
            "Petalburg Woods","Rusturf Tunnel","Granite Cave","Mt. Chimney",
            "Jagged Pass","Fiery Path","New Mauville","Abandoned Ship",
            "Weather Institute","Mt. Pyre","Cave of Origin","Seafloor Cavern",
            "Shoal Cave","Sky Pillar","Meteor Falls","Safari Zone",
            "Victory Road","Desert Ruins","Island Cave","Ancient Tomb",
            "Mirage Tower","Desert Underpass","Artisan Cave","Magma Hideout",
            "Altering Cave",
            // Special / Event
            "Roaming Pokemon","Southern Island","Birth Island","Faraway Island",
            "Navel Rock","Terra Cave","Marine Cave",
        ],

        ["Fire Red / Leaf Green"] =
        [
            // Kanto routes
            "Route 1","Route 2","Route 3","Route 4","Route 5",
            "Route 6","Route 7","Route 8","Route 9","Route 10",
            "Route 11","Route 12","Route 13","Route 14","Route 15",
            "Route 16","Route 17","Route 18","Route 19","Route 20",
            "Route 21","Route 22","Route 23","Route 24","Route 25",
            // Kanto towns & cities
            "Pallet Town","Viridian City","Pewter City","Cerulean City",
            "Vermilion City","Lavender Town","Celadon City","Fuchsia City",
            "Saffron City","Cinnabar Island",
            // Kanto dungeons & landmarks
            "Cerulean Cave","Diglett's Cave","Mt. Moon","Rock Tunnel",
            "Pokémon Tower","Power Plant","Safari Zone","Seafoam Islands",
            "Silph Co.","Victory Road","Viridian Forest",
            // Sevii Islands
            "One Island","Two Island","Three Island","Four Island",
            "Five Island","Six Island","Seven Island",
            "Altering Cave","Berry Forest","Cape Brink","Icefall Cave",
            "Lost Cave","Mt. Ember","Pattern Bush","Ruin Valley",
            "Tanoby Ruins","Water Labyrinth","Water Path",
            // Additional Sevii Islands areas
            "Bond Bridge","Canyon Entrance","Five Isle Meadow","Green Path",
            "Kindle Road","Memorial Pillar","Outcast Island","Resort Gorgeous",
            "Sevault Canyon","Three Isle Port","Trainer Tower","Treasure Beach",
            // Tanoby Ruins chambers
            "Monean Chamber","Dilford Chamber","Liptoo Chamber","Rixy Chamber",
            "Scufib Chamber","Viapos Chamber","Weepth Chamber",
            "Ss Anne",
            // Special / Event
            "Roaming Pokemon","Navel Rock","Birth Island","Pokémon Mansion",
        ],

        ["Diamond / Pearl / Platinum"] =
        [
            // Sinnoh routes
            "Route 201","Route 202","Route 203","Route 204","Route 205",
            "Route 206","Route 207","Route 208","Route 209","Route 210",
            "Route 211","Route 212","Route 213","Route 214","Route 215",
            "Route 216","Route 217","Route 218","Route 219","Route 220",
            "Route 221","Route 222","Route 223","Route 224","Route 225",
            "Route 226","Route 227","Route 228","Route 229","Route 230",
            // Cities & towns
            "Twinleaf Town","Sandgem Town","Jubilife City","Oreburgh City",
            "Floaroma Town","Eterna City","Hearthome City","Solaceon Town",
            "Veilstone City","Pastoria City","Celestic Town","Canalave City",
            "Snowpoint City","Sunyshore City","Fight Area","Survival Area","Resort Area",
            // Dungeons & landmarks
            "Oreburgh Gate","Oreburgh Mine","Ravaged Path","Floaroma Meadow",
            "Valley Windworks","Eterna Forest","Old Chateau","Wayward Cave",
            "Mt. Coronet","Solaceon Ruins","Iron Island","Fuego Ironworks",
            "Lake Valor","Lake Verity","Lake Acuity",
            "Sendoff Spring","Spring Path","Turnback Cave","Snowpoint Temple",
            "Victory Road","Pokémon League","Stark Mountain","Great Marsh","Trophy Garden",
            "Fullmoon Island","Newmoon Island",
            "Acuity Lakefront","Valor Lakefront",
            "Lost Tower","Maniac Tunnel","Ruin Maniac Cave",
            // Special
            "Routes (DPP Roaming)",
        ],

        ["Heart Gold / Soul Silver"] =
        [
            // Johto routes
            "Route 29","Route 30","Route 31","Route 32","Route 33",
            "Route 34","Route 35","Route 36","Route 37","Route 38",
            "Route 39","Route 40","Route 41","Route 42","Route 43",
            "Route 44","Route 45","Route 46","Route 47","Route 48",
            // Johto locations
            "Azalea Town","Bell Tower","Blackthorn City","Burned Tower",
            "Cherrygrove City","Cianwood City","Cliff Cave","Dark Cave",
            "Dragon's Den","Ecruteak City","Embedded Tower","Goldenrod City",
            "Ice Path","Ilex Forest","Lake of Rage","Mahogany Town",
            "Mt. Mortar","Mt. Silver","National Park","New Bark Town",
            "Olivine City","Ruins of Alph","Safari Zone",
            "Sinjoh Ruins","Slowpoke Well","Sprout Tower","Union Cave",
            "Violet City","Whirl Islands",
            // Kanto routes
            "Route 1","Route 2","Route 3","Route 4","Route 5",
            "Route 6","Route 7","Route 8","Route 9","Route 10",
            "Route 11","Route 12","Route 13","Route 14","Route 15",
            "Route 16","Route 17","Route 18","Route 19","Route 20",
            "Route 21","Route 22","Route 23","Route 24","Route 25",
            "Route 26","Route 27","Route 28",
            // Kanto locations
            "Celadon City","Cerulean Cave","Cerulean City","Cinnabar Island",
            "Diglett's Cave","Fuchsia City","Mt. Moon","Pallet Town",
            "Pewter City","Power Plant","Rock Tunnel","Seafoam Islands","Silph Co.",
            "Tohjo Falls","Vermilion City","Victory Road","Viridian City",
            "Viridian Forest",
            // Special
            "Roaming Pokemon",
        ],

        ["Black / White"] =
        [
            // Unova routes
            "Route 1","Route 2","Route 3","Route 4","Route 5",
            "Route 6","Route 7","Route 8","Route 9","Route 10",
            "Route 11","Route 12","Route 13","Route 14","Route 15",
            "Route 16","Route 17","Route 18",
            // Cities & towns
            "Nuvema Town","Accumula Town","Striaton City","Nacrene City",
            "Castelia City","Nimbasa City","Driftveil City","Mistralton City",
            "Icirrus City","Opelucid City","Lacunosa Town","Undella Town",
            "Black City","White Forest",
            // Dungeons & landmarks
            "Wellspring Cave","Pinwheel Forest","Lostlorn Forest",
            "Desert Resort","Relic Castle","Cold Storage",
            "Chargestone Cave","Mistralton Cave","Twist Mountain",
            "Dragonspiral Tower","Moor of Icirrus","Challenger's Cave",
            "Giant Chasm","Victory Road",
            // Special areas
            "Abundant Shrine","Village Bridge","Marvelous Bridge",
            "P2 Laboratory","Liberty Garden",
            "Dreamyard","N's Castle","Celestial Tower",
            "Guidance Chamber","Trial Chamber","Undella Bay",
            // Special
            "Routes (BW Roaming)",
        ],

        ["Black 2 / White 2"] =
        [
            // Unova routes
            "Route 1","Route 2","Route 3","Route 4","Route 5",
            "Route 6","Route 7","Route 8","Route 9",
            "Route 11","Route 12","Route 13","Route 14","Route 15",
            "Route 16","Route 17","Route 18","Route 19","Route 20",
            "Route 21","Route 22","Route 23",
            // Cities & towns
            "Nuvema Town","Accumula Town","Striaton City","Nacrene City",
            "Castelia City","Nimbasa City","Driftveil City","Mistralton City",
            "Icirrus City","Opelucid City","Lacunosa Town","Undella Town",
            "Aspertia City","Floccesy Town","Virbank City","Lentimas Town",
            "Humilau City","Black City","White Forest",
            // Dungeons & landmarks
            "Floccesy Ranch","Virbank Complex","Castelia Sewers",
            "Wellspring Cave","Pinwheel Forest","Lostlorn Forest",
            "Desert Resort","Relic Castle","Relic Passage",
            "Reversal Mountain","Strange House",
            "Clay Tunnel","Chargestone Cave","Mistralton Cave","Twist Mountain",
            "Dragonspiral Tower","Moor of Icirrus","Seaside Cave",
            "Guidance Chamber","Giant Chasm","Victory Road",
            "Underground Ruins","Rocky Mountain Room","Glacier Room","Iron Room",
            // Special areas
            "Abundant Shrine","Village Bridge","Marvelous Bridge",
            "P2 Laboratory","Liberty Garden","Dreamyard","Celestial Tower",
            "Nature Sanctuary","Undella Bay",
            "Plasma Frigate","N's Castle",
            // Special
            "Routes (B2W2 Roaming)",
        ],
    };
}
