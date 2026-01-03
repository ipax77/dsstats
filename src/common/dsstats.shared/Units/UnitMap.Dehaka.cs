using System.Collections.Frozen;

namespace dsstats.shared.Units;

public static partial class UnitMap
{
    private static readonly FrozenDictionary<string, string> DehakaUnitMap =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // v3 names
            ["DehakaHero"] = "Dehaka",
            ["DehakaPrimalHydralisk"] = "Primal Hydralisk",
            ["DehakaPrimalRavasaur"] = "Ravasaur",
            ["DehakaImpaler"] = "Impaler",
            ["DehakaCreeperHost"] = "Creeper Host",
            ["DehakaPrimalIgniter"] = "Primal Igniter",
            ["DehakaPrimalMutalisk"] = "Primal Mutalisk",
            ["DehakaPrimalZergling"] = "Primal Zergling",
            ["DehakaPrimalUltralisk"] = "Primal Ultralisk",
            ["DehakaPrimalRoach"] = "Primal Roach",
            ["DehakaTyrannozor"] = "Tyrannozor",
            ["DehakaPrimalHost"] = "Primal Host",
            ["DehakaPrimalGuardian"] = "Primal Guardian",
            ["DehakaPrimalHostLocust"] = "Primal Locusts",
            ["StrikeWeaponryLevel1Dummy"] = "Primal Wurm",

            // v2 names
            ["CreeperHost"] = "Creeper Host",
            ["Dehaka"] = "Dehaka",
            ["Impaler"] = "Impaler",
            ["PrimalGuardian"] = "Primal Guardian",
            ["PrimalHost"] = "Primal Host",
            ["PrimalHydralisk"] = "Primal Hydralisk",
            ["PrimalIgniter"] = "Primal Igniter",
            ["PrimalMutalisk"] = "Primal Mutalisk",
            ["PrimalRavasaur"] = "Ravasaur",
            ["PrimalRoach"] = "Primal Roach",
            ["PrimalUltralisk"] = "Primal Ultralisk",
            ["PrimalZergling"] = "Primal Zergling",
            ["Tyrannozor"] = "Tyrannozor",

            // normalized names
            ["Creeper Host"] = "Creeper Host",
            ["Explosive Creeper"] = "Explosive Creeper",
            ["Greater Primal Wurm"] = "Greater Primal Wurm",
            ["Primal Guardian"] = "Primal Guardian",
            ["Primal Host"] = "Primal Host",
            ["Primal Hydralisk"] = "Primal Hydralisk",
            ["Primal Igniter"] = "Primal Igniter",
            ["Primal Locusts"] = "Primal Locusts",
            ["Primal Mutalisk"] = "Primal Mutalisk",
            ["Primal Roach"] = "Primal Roach",
            ["Primal Ultralisk"] = "Primal Ultralisk",
            ["Primal Wurm"] = "Primal Wurm",
            ["Primal Zergling"] = "Primal Zergling",

        }.ToFrozenDictionary();

    private static string GetDehakaUnitName(string name) =>
        DehakaUnitMap.TryGetValue(name, out var mapped) ? mapped : name;
}
