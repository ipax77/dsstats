using System.Collections.Frozen;

namespace dsstats.shared.Units;

public static partial class UnitMap
{
    private static readonly FrozenDictionary<string, string> ArtanisUnitMap =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // v3 names
            ["HonorGuard"] = "Honor Guard",
            ["DragoonStarlight"] = "Dragoon",
            ["ImmortalArtanis"] = "Immortal",
            ["ArtanisObserver"] = "Observer",
            ["HighArchon"] = "High Archon",
            ["PurifierTempest"] = "Purifier Tempest",
            ["PhoenixArtanis"] = "Phoenix",
            ["ReaverStarlight"] = "Reaver",
            ["HighTemplarArtanis"] = "High Templar",
            ["Observer"] = "Observer",

            // v2 names
            ["Dragoon"] = "Dragoon",
            // HighArchon already mapped
            ["HighTemplar"] = "High Templar",
            // HonorGuard already mapped
            ["Immortal"] = "Immortal",
            // Observer already mapped
            ["Phoenix"] = "Phoenix",
            // PurifierTempest already mapped
            ["Reaver"] = "Reaver",

            // normalized names
            ["High Archon"] = "High Archon",
            ["Honor Guard"] = "Honor Guard",
            ["Photon Cannon"] = "Photon Cannon",
            ["Purifier Tempest"] = "Purifier Tempest",

        }.ToFrozenDictionary();

    private static string GetArtanisUnitName(string name) =>
        ArtanisUnitMap.TryGetValue(name, out var mapped) ? mapped : name;
}
