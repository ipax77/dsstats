using System.Collections.Frozen;

namespace dsstats.shared.Units;

public static partial class UnitMap
{
    private static readonly FrozenDictionary<string, string> HornerUnitMap =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // v3 names
            ["HornerTheiaRaven"] = "Theia Raven",
            ["HornerHellbat"] = "Hellbat",
            ["HornerHellion"] = "Hellion",
            ["HornerDeimosVikingFighter"] = "Deimos Viking",
            ["HornerAsteriaWraith"] = "Asteria Wraith",
            ["HornerAssaultGalleonUpgraded"] = "Assault Galleon",
            ["HornerReaper"] = "Reaper",
            ["HornerSovereignBattlecruiser"] = "Sovereign Battlecruiser",
            ["HornerWidowMine"] = "Widow Mine",
            ["HornerAssaultGalleon"] = "Assault Galleon",
            ["HornerStrikeFighter"] = "Strike Fighter",
            ["HornerDeimosVikingAssault"] = "Deimos Viking",

            // v2 names
            ["AssaultGalleon"] = "Assault Galleon",
            ["AsteriaWraith"] = "Asteria Wraith",
            ["DeimosViking"] = "Deimos Viking",
            ["StrikeFighter"] = "Strike Fighter",
            ["TheiaRaven"] = "Theia Raven",
            ["WidowMine"] = "Widow Mine",
            ["SovereignBattlecruiser"] = "Sovereign Battlecruiser",


            // normalized names
            ["Assault Galleon"] = "Assault Galleon",
            ["Asteria Wraith"] = "Asteria Wraith",
            ["Deimos Viking"] = "Deimos Viking",
            ["Mag Mine"] = "Mag Mine",
            ["Missile Turret"] = "Missile Turret",
            ["Sovereign Battlecruiser"] = "Sovereign Battlecruiser",
            ["Strike Fighter"] = "Strike Fighter",
            ["Theia Raven"] = "Theia Raven",
            ["Widow Mine"] = "Widow Mine",

        }.ToFrozenDictionary();

    private static string GetHornerUnitName(string name) =>
        HornerUnitMap.TryGetValue(name, out var mapped) ? mapped : name;
}
