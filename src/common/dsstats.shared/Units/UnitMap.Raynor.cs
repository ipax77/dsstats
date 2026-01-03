using System.Collections.Frozen;

namespace dsstats.shared.Units;

public static partial class UnitMap
{
    private static readonly FrozenDictionary<string, string> RaynorUnitMap =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // v3 names
            ["FirebatStarlight"] = "Firebat",
            ["MedicStarlight"] = "Medic",
            ["MarineLightweight"] = "Marine",
            ["DuskWing"] = "Dusk Wings",
            ["VikingFighter"] = "Viking",
            ["BattlecruiserRaynor"] = "Battlecruiser",
            ["VultureStarlight"] = "Vulture",
            ["RaynorHyperion"] = "Hyperion",
            ["VikingAssault"] = "Viking",

            // v2 names
            ["SpiderMine"] = "Spider Mine",

            // normalized names
            ["Dusk Wings"] = "Dusk Wings",
            ["Missile Turret"] = "Missile Turret",
            ["Siege Tank"] = "Siege Tank",
            ["Spider Mine"] = "Spider Mine",

        }.ToFrozenDictionary();

    private static string GetRaynorUnitName(string name) =>
        RaynorUnitMap.TryGetValue(name, out var mapped) ? mapped : name;
}
