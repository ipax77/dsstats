using System.Collections.Frozen;

namespace dsstats.shared.Units;

public static partial class UnitMap
{
    private static readonly FrozenDictionary<string, string> KerriganUnitMap =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // v3 names
            ["HeroKerriganWaveUnit"] = "Kerrigan",
            ["HydraliskKerrigan"] = "Hydralisk",
            ["Torrasque"] = "Torrasque",
            ["RaptorlingLightweight"] = "Raptorling",
            ["KerriganOverseer"] = "Overseer",
            ["KerriganQueen"] = "Queen",
            ["BroodMutalisk"] = "Brood Mutalisk",
            ["LurkerMP"] = "Lurker",
            ["BroodLordKerrigan"] = "Brood Lord",
            ["Raptorling"] = "Raptorling",

            // v2 names
            ["BroodLord"] = "Brood Lord",
            ["Hydralisk"] = "Hydralisk",
            ["Kerrigan"] = "Kerrigan",
            ["Lurker"] = "Lurker",
            ["Queen"] = "Queen",

            // normalized names
            ["Brood Lord"] = "Brood Lord",
            ["Brood Mutalisk"] = "Brood Mutalisk",
            ["Broodling"] = "Broodling",
            ["Spine Crawler"] = "Spine Crawler",
            ["Spore Crawler"] = "Spore Crawler",

        }.ToFrozenDictionary();

    private static string GetKerriganUnitName(string name) =>
        KerriganUnitMap.TryGetValue(name, out var mapped) ? mapped : name;
}
