using System.Collections.Frozen;

namespace dsstats.shared.Units;

public static partial class UnitMap
{
    private static readonly FrozenDictionary<string, string> ZeratulUnitMap =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // v3 names
            ["Telbrus"] = "Telbrus",
            ["VoidTemplar"] = "Void Templar",
            ["XelNagaAbrogator"] = "Xel'Naga Abrogator",
            ["XelNagaAmbusher"] = "Xel'Naga Ambusher",
            ["XelNagaEnforcer"] = "Xel'Naga Enforcer",
            ["XelNagaShieldguard"] = "Xel'Naga Shieldguard",
            ["XelNagaWatcher"] = "Xel'Naga Watcher",
            ["Zeratul"] = "Zeratul",

            // normalized names
            ["Honor Guard"] = "Honor Guard",
            ["Void Templar"] = "Void Templar",
            ["Xel'Naga Abrogator"] = "Xel'Naga Abrogator",
            ["Xel'Naga Ambusher"] = "Xel'Naga Ambusher",
            ["Xel'Naga Enforcer"] = "Xel'Naga Enforcer",
            ["Xel'Naga Shieldguard"] = "Xel'Naga Shieldguard",
            ["Xel'Naga Watcher"] = "Xel'Naga Watcher",

        }.ToFrozenDictionary();

    private static string GetZeratulUnitName(string name) =>
        ZeratulUnitMap.TryGetValue(name, out var mapped) ? mapped : name;
}
