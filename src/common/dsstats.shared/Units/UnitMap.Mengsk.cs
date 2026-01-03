using System.Collections.Frozen;

namespace dsstats.shared.Units;

public static partial class UnitMap
{
    private static readonly FrozenDictionary<string, string> MengskUnitMap =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // v3 names
            ["RavenMengsk"] = "Imperial Witness",
            ["SiegeTankMengsk"] = "Shock Division",
            ["ZerglingMengsk"] = "Zergling",
            ["MarauderMengsk"] = "Aegis Guard",
            ["WarHound"] = "Warhound",
            ["TrooperMengskFlamethrower"] = "Flamethrower Dominion Trooper",
            ["VikingMengskFighter"] = "Sky Fury",
            ["TrooperMengskImproved"] = "LMG Dominion Trooper",
            ["MedivacMengsk"] = "Imperial Intercessor",
            ["GhostMengsk"] = "Emperor's Shadow",
            ["TrooperMengskAA"] = "Hailstorm Dominion Trooper",
            ["ThorMengsk"] = "Blackhammer",
            ["UltraliskMengsk"] = "Ultralisk",
            ["TrooperMengsk"] = "Dominion Trooper",
            ["MutaliskMengsk"] = "Mutalisk",
            ["BattlecruiserMengsk"] = "Pride of Augustgrad",
            ["VikingMengskAssault"] = "Sky Fury",

            // v2 names
            ["Battlecruiser"] = "Pride of Augustgrad",
            ["Ghost"] = "Emperor's Shadow",
            ["Marauder"] = "Aegis Guard",
            ["Medivac"] = "Imperial Intercessor",
            ["Raven"] = "Imperial Witness",
            ["SiegeTank"] = "Shock Division",
            ["SkyFury"] = "Sky Fury",
            ["Thor"] = "Blackhammer",
            ["Trooper"] = "Dominion Trooper",
            ["TrooperMengskAA"] = "Hailstorm Dominion Trooper",
            ["TrooperMengskFlamethrower"] = "Flamethrower Dominion Trooper",
            ["TrooperMengskImproved"] = "LMG Dominion Trooper",
            ["WarHound"] = "Warhound",
            ["Zergling"] = "Zergling",


            // normalized names
            ["Aegis Guard"] = "Aegis Guard",
            ["Blackhammer"] = "Blackhammer",
            ["Dominion Trooper"] = "Dominion Trooper",
            ["Earthsplitter Ordnance"] = "Earthsplitter Ordnance",
            ["Emperor's Shadow"] = "Emperor's Shadow",
            ["Flamethrower Dominion Trooper"] = "Flamethrower Dominion Trooper",
            ["Hailstorm Dominion Trooper"] = "Hailstorm Dominion Trooper",
            ["Imperial Intercessor"] = "Imperial Intercessor",
            ["Imperial Witness"] = "Imperial Witness",
            ["LMG Dominion Trooper"] = "LMG Dominion Trooper",
            ["Missile Turret"] = "Missile Turret",
            ["Pride of Augustgrad"] = "Pride of Augustgrad",
            ["Shock Division"] = "Shock Division",
            ["Sky Fury"] = "Sky Fury",
            ["Supply Bunker"] = "Supply Bunker",

        }.ToFrozenDictionary();

    private static string GetMengskUnitName(string name) =>
        MengskUnitMap.TryGetValue(name, out var mapped) ? mapped : name;
}
