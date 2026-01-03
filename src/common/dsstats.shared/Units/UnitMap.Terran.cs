using System.Collections.Frozen;

namespace dsstats.shared.Units;

public static partial class UnitMap
{
    private static readonly FrozenDictionary<string, string> TerranUnitMap =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // v3 names
            ["MarineLightweight"] = "Marine",
            ["Marauder"] = "Marauder",
            ["Raven"] = "Raven",
            ["Liberator"] = "Liberator",
            ["Reaper"] = "Reaper",
            ["VikingFighter"] = "Viking",
            ["ThorAP"] = "Thor",
            ["WidowMine"] = "Widow Mine",
            ["SiegeTank"] = "Siege Tank",
            ["HellionTank"] = "Hellion",
            ["Medivac"] = "Medivac",
            ["Battlecruiser"] = "Battlecruiser",
            ["Ghost"] = "Ghost",
            ["Banshee"] = "Banshee",
            ["Cyclone"] = "Cyclone",
            ["VikingAssault"] = "Viking",
            ["Thor"] = "Thor",
            ["GhostAlternate"] = "Ghost",
            ["Hellion"] = "Hellion",

            // v2 names
            ["Marine"] = "Marine",
            ["Viking"] = "Viking",
            ["GhostNova"] = "Ghost",

            // normalized names
            ["Hellbat"] = "Hellbat",
            ["Siege Tank"] = "Siege Tank",
            ["Widow Mine"] = "Widow Mine",
        }.ToFrozenDictionary();

    private static string GetTerranUnitName(string name) =>
        TerranUnitMap.TryGetValue(name, out var mapped) ? mapped : name;
}
