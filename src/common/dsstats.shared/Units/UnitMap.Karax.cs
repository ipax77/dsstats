using System.Collections.Frozen;

namespace dsstats.shared.Units;

public static partial class UnitMap
{
    private static readonly FrozenDictionary<string, string> KaraxUnitMap =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // v3 names
            ["Mirage"] = "Mirage",
            ["SentinelStarlight"] = "Sentinel",
            ["Annihilator"] = "Annihilator",
            ["AiurCarrier"] = "Support Carrier",
            ["KaraxObserver"] = "Observer",
            ["Energizer"] = "Energizer",
            ["PurifierColossus"] = "Purifier Colossus",
            ["Observer"] = "Observer",

            // v2 names
            ["AiurCarrier"] = "Support Carrier",
            ["PurifierColossus"] = "Purifier Colossus",
            ["Sentinel"] = "Sentinel",

            // normalized names
            ["Khaydarin Monolith"] = "Khaydarin Monolith",
            ["Photon Cannon"] = "Photon Cannon",
            ["Purifier Colossus"] = "Purifier Colossus",
            ["Shield Battery"] = "Shield Battery",
            ["Support Carrier"] = "Support Carrier",

        }.ToFrozenDictionary();

    private static string GetKaraxUnitName(string name) =>
        KaraxUnitMap.TryGetValue(name, out var mapped) ? mapped : name;
}
