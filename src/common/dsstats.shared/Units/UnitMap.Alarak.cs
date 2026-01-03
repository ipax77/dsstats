using System.Collections.Frozen;

namespace dsstats.shared.Units;

public static partial class UnitMap
{
    private static readonly FrozenDictionary<string, string> AlarakUnitMap =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // Alarak (hero / wave unit)
            ["Alarak"] = "Alarak",
            ["HeroAlarakWaveUnit"] = "Alarak",

            // Ascendant
            ["Ascendant"] = "Ascendant",
            ["AscendantStarlight"] = "Ascendant",

            // Destroyer
            ["Destroyer"] = "Destroyer",

            // Havoc
            ["Havoc"] = "Havoc",
            ["HavocStarlight"] = "Havoc",

            // Slayer
            ["Slayer"] = "Slayer",

            // Supplicant
            ["Supplicant"] = "Supplicant",
            ["SupplicantStarlight"] = "Supplicant",

            // Vanguard
            ["Vanguard"] = "Vanguard",

            // Wrathwalker
            ["Wrathwalker"] = "Wrathwalker",

            // War Prism
            ["WarPrism"] = "War Prism",

            // Tal'darim Mothership
            ["MothershipTaldarim"] = "Tal'darim Mothership",

            // Photon Cannon (note: appears only in normalized list)
            ["PhotonCannon"] = "Photon Cannon",
        }
        .ToFrozenDictionary();

    private static string GetAlarakUnitName(string name) =>
        AlarakUnitMap.TryGetValue(name, out var mapped) ? mapped : name;
}
