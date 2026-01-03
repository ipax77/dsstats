using System.Collections.Frozen;

namespace dsstats.shared.Units;

public static partial class UnitMap
{
    private static readonly FrozenDictionary<string, string> ZergUnitMap =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // v3 names
            ["Hydralisk"] = "Hydralisk",
            ["Infestor"] = "Infestor",
            ["ZerglingLightweight"] = "Zergling",
            ["Overseer"] = "Overseer",
            ["Corruptor"] = "Corruptor",
            ["LurkerMP"] = "Lurker",
            ["Roach"] = "Roach",
            ["Queen"] = "Queen",
            ["Mutalisk"] = "Mutalisk",
            ["SwarmHostMP"] = "Swarm Host",
            ["Ultralisk"] = "Ultralisk",
            ["Ravager"] = "Ravager",
            ["Baneling"] = "Baneling",
            ["Viper"] = "Viper",
            ["BroodLord"] = "Brood Lord",
            ["LocustMPPrecursor"] = "Locust",

            // v2 names
            ["Zergling"] = "Zergling",
            ["SwarmHost"] = "Swarm Host",
            ["BroodLord"] = "Brood Lord",

            // normalized names
            ["Swarm Host"] = "Swarm Host",
        }.ToFrozenDictionary();

    private static string GetZergUnitName(string name) =>
        ZergUnitMap.TryGetValue(name, out var mapped) ? mapped : name;
}
