using System.Collections.Frozen;

namespace dsstats.shared.Units;

public static partial class UnitMap
{
    private static readonly FrozenDictionary<string, string> StetmannUnitMap =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // v3 names
            ["ZerglingStetmann"] = "Mecha Zergling",
            ["HydraliskStetmann"] = "Mecha Hydralisk",
            ["UltraliskStetmann"] = "Mecha Ultralisk",
            ["SuperGaryStetmann"] = "Super Gary",
            ["LurkerStetmann"] = "Mecha Lurker",
            ["BanelingStetmann"] = "Mecha Baneling",
            ["GaryStetmann"] = "Gary",
            ["CorruptorStetmann"] = "Mecha Corruptor",
            ["OverseerStetmann"] = "Mecha Overseer",
            ["InfestorStetmann"] = "Mecha Infestor",
            ["InterceptorStetmann"] = "Mecha Broodling",
            ["BroodLordStetmann"] = "Mecha Battlecarrier Lord",

            // v2 names
            ["Baneling"] = "Mecha Baneling",
            ["BroodLord"] = "Mecha Battlecarrier Lord",
            ["Corruptor"] = "Mecha Corruptor",
            ["Hydralisk"] = "Mecha Hydralisk",
            ["Infestor"] = "Mecha Infestor",
            ["Lurker"] = "Mecha Lurker",
            ["Overseer"] = "Mecha Overseer",
            ["SuperGary"] = "Super Gary",
            ["Ultralisk"] = "Mecha Ultralisk",
            ["Zergling"] = "Mecha Zergling",

            // normalized names
            ["Mecha Baneling"] = "Mecha Baneling",
            ["Mecha Battlecarrier Lord"] = "Mecha Battlecarrier Lord",
            ["Mecha Broodling"] = "Mecha Broodling",
            ["Mecha Corruptor"] = "Mecha Corruptor",
            ["Mecha Hydralisk"] = "Mecha Hydralisk",
            ["Mecha Infestor"] = "Mecha Infestor",
            ["Mecha Lurker"] = "Mecha Lurker",
            ["Mecha Overseer"] = "Mecha Overseer",
            ["Mecha Ravager"] = "Mecha Ravager",
            ["Mecha Roach"] = "Mecha Roach",
            ["Mecha Spine Crawler"] = "Mecha Spine Crawler",
            ["Mecha Spore Crawler"] = "Mecha Spore Crawler",
            ["Mecha Ultralisk"] = "Mecha Ultralisk",
            ["Mecha Zergling"] = "Mecha Zergling",
            ["Super Gary"] = "Super Gary",

        }.ToFrozenDictionary();

    private static string GetStetmannUnitName(string name) =>
        StetmannUnitMap.TryGetValue(name, out var mapped) ? mapped : name;
}
