using System.Collections.Frozen;

namespace dsstats.shared.Units;

public static partial class UnitMap
{
    private static readonly FrozenDictionary<string, string> TychusUnitMap =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // v3 names
            ["TychusTychus"] = "Tychus",
            ["TychusRattlesnake"] = "Kev \"Rattlesnake\" West",
            ["TychusCannonball"] = "Rob \"Cannonball\" Boswell",
            ["TychusNikara"] = "Lt.Layna Nikara",
            ["TychusBlaze"] = "Miles \"Blaze\" Lewis",
            ["TychusSam"] = "Crooked Sam",
            ["TychusSirius"] = "James \"Sirius\" Sykes",
            ["TychusVega"] = "Vega",
            ["TychusNux"] = "Nux",

            // v2 names
            ["Blaze"] = "Miles \"Blaze\" Lewis",
            ["Cannonball"] = "Rob \"Cannonball\" Boswell",
            ["Nikara"] = "Lt.Layna Nikara",
            ["Rattlesnake"] = "Kev \"Rattlesnake\" West",
            ["Sam"] = "Crooked Sam",
            ["Sirius"] = "James \"Sirius\" Sykes",

            // normalized names
            ["Auto-Turret"] = "Auto-Turret",
            ["Crooked Sam"] = "Crooked Sam",
            ["James \"Sirius\" Sykes"] = "James \"Sirius\" Sykes",
            ["Kev \"Rattlesnake\" West"] = "Kev \"Rattlesnake\" West",
            ["Lt.Layna Nikara"] = "Lt.Layna Nikara",
            ["Miles \"Blaze\" Lewis"] = "Miles \"Blaze\" Lewis",
            ["Rob \"Cannonball\" Boswell"] = "Rob \"Cannonball\" Boswell",
            ["Warhound Turret"] = "Warhound Turret",

        }.ToFrozenDictionary();

    private static string GetTychusUnitName(string name) =>
        TychusUnitMap.TryGetValue(name, out var mapped) ? mapped : name;
}