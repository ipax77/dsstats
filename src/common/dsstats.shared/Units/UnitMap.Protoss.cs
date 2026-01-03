using System.Collections.Frozen;

namespace dsstats.shared.Units;

public static partial class UnitMap
{
    private static readonly FrozenDictionary<string, string> ProtossUnitMap =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // v3 names
            ["Sentry"] = "Sentry",
            ["Stalker"] = "Stalker",
            ["Immortal"] = "Immortal",
            ["Observer"] = "Observer",
            ["Disruptor"] = "Disruptor",
            ["Zealot"] = "Zealot",
            ["Adept"] = "Adept",
            ["Archon"] = "Archon",
            ["VoidRay"] = "Void Ray",
            ["HighTemplar"] = "High Templar",
            ["Carrier"] = "Carrier",
            ["Phoenix"] = "Phoenix",
            ["Colossus"] = "Colossus",
            ["Tempest"] = "Tempest",
            ["Mothership"] = "Mothership",
            ["Oracle"] = "Oracle",
            ["DarkTemplar"] = "Dark Templar",

            // v2 names (all seem to map directly to normalized or v3)

            // normalized names
            ["Dark Templar"] = "Dark Templar",
            ["High Templar"] = "High Templar",
            ["Void Ray"] = "Void Ray",
        }.ToFrozenDictionary();

    private static string GetProtossUnitName(string name) =>
        ProtossUnitMap.TryGetValue(name, out var mapped) ? mapped : name;
}
