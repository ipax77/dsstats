using System.Collections.Frozen;

namespace dsstats.shared.Units;

public static partial class UnitMap
{
    private static readonly FrozenDictionary<string, string> VorazunUnitMap =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // v3 names
            ["StalkerVorazun"] = "Stalker",
            ["VorazunOracle"] = "Oracle",
            ["VoidRayVorazun"] = "Void Ray",
            ["DarkTemplarVorazun"] = "Dark Templar",
            ["DarkArchonStarlight"] = "Dark Archon",

            // v2 names
            ["DarkArchon"] = "Dark Archon",
            ["DarkTemplar"] = "Dark Templar",
            ["VoidRay"] = "Void Ray",

            // normalized names
            ["Dark Archon"] = "Dark Archon",
            ["Dark Templar"] = "Dark Templar",
            ["Photon Cannon"] = "Photon Cannon",
            ["Shadow Guard"] = "Shadow Guard",
            ["Void Ray"] = "Void Ray",

        }.ToFrozenDictionary();

    private static string GetVorazunUnitName(string name) =>
        VorazunUnitMap.TryGetValue(name, out var mapped) ? mapped : name;
}
