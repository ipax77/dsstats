using System.Collections.Frozen;

namespace dsstats.shared.Units;

public static partial class UnitMap
{
    private static readonly FrozenDictionary<string, string> StukovUnitMap =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // v3 names
            ["StukovInfestedBunker"] = "Infested Bunker",
            ["InfestedCivilianStarlight"] = "Infested Civilian",
            ["InfestedDiamondbackStarlight"] = "Infested Diamondback",
            ["InfestedBansheeStarlight"] = "Infested Banshee",
            ["InfestedSiegeTankStarlight"] = "Infested Siege Tank",
            ["InfestedMarineStarlight"] = "Infested Marine",
            ["VolatileInfestedStarlight"] = "Volatile Infested",
            ["StukovAleksander"] = "Aleksander",
            ["InfestedLiberatorStarlight"] = "Infested Liberator",
            ["ApocaliskStarlight"] = "Apocalisk",
            ["BroodQueenStarlight"] = "Brood Queen",
            ["InfestedLiberatorStarlightViralSwarm"] = "Infested Liberator",
            ["InfestedCivilianLightweight"] = "Infested Civilian",

            // v2 names
            ["InfestedBanshee"] = "Infested Banshee",
            ["InfestedBunker"] = "Infested Bunker",
            ["InfestedCivilian"] = "Infested Civilian",
            ["InfestedDiamondback"] = "Infested Diamondback",
            ["InfestedLiberator"] = "Infested Liberator",
            ["InfestedLiberatorViralSwarm"] = "Infested Liberator",
            ["InfestedMarine"] = "Infested Marine",
            ["InfestedSiegeTank"] = "Infested Siege Tank",
            ["VolatileInfested"] = "Volatile Infested",

            // normalized names
            ["Brood Queen"] = "Brood Queen",
            ["Infested Banshee"] = "Infested Banshee",
            ["Infested Bunker"] = "Infested Bunker",
            ["Infested Civilian"] = "Infested Civilian",
            ["Infested Diamondback"] = "Infested Diamondback",
            ["Infested Liberator"] = "Infested Liberator",
            ["Infested Marine"] = "Infested Marine",
            ["Infested Missile Turret"] = "Infested Missile Turret",
            ["Infested Siege Tank"] = "Infested Siege Tank",
            ["Volatile Infested"] = "Volatile Infested",

        }.ToFrozenDictionary();

    private static string GetStukovUnitName(string name) =>
        StukovUnitMap.TryGetValue(name, out var mapped) ? mapped : name;
}
