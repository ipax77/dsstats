using System.Collections.Frozen;

namespace dsstats.shared.Units;

public static partial class UnitMap
{
    private static readonly FrozenDictionary<string, string> ZagaraUnitMap =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // v3 names
            ["HeroZagaraWaveUnit"] = "Zagara",
            ["InfestedAbomination"] = "Aberration",
            ["HunterKillerZagara"] = "Hunter Killer",
            ["SwarmlingLightweight"] = "Swarmling",
            ["OverseerZagara"] = "Overseer",
            ["ZagaraRoach"] = "Roach",
            ["ZagaraQueen"] = "Queen",
            ["CorruptorZagara"] = "Corruptor",

            // v2 names
            ["HunterKiller"] = "Hunter Killer",

            // normalized names
            ["Bile Launcher"] = "Bile Launcher",
            ["Hunter Killer"] = "Hunter Killer",
            ["Spine Crawler"] = "Spine Crawler",
            ["Splitterling Spawn"] = "Splitterling Spawn",
            ["Spore Crawler"] = "Spore Crawler",

        }.ToFrozenDictionary();

    private static string GetZagaraUnitName(string name) =>
        ZagaraUnitMap.TryGetValue(name, out var mapped) ? mapped : name;
}
