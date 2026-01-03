using System.Collections.Frozen;

namespace dsstats.shared.Units;

public static partial class UnitMap
{
    private static readonly FrozenDictionary<string, string> NovaUnitMap =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // v3 names
            ["HeroNovaWaveUnit"] = "Nova",
            ["StrikeGoliath"] = "Strike Goliath",
            ["HeavySiegeTank"] = "Heavy Siege Tank",
            ["EliteMarine"] = "Elite Marine",
            ["RavenTypeII"] = "Raven Type-II",
            ["HellbatRanger"] = "Hellbat Ranger",
            ["MarauderCommando"] = "Marauder Commando",
            ["RaidLiberator"] = "Raid Liberator",
            ["SpecOpsGhost"] = "Spec Ops Ghost",
            ["HoloDecoy"] = "Holo Decoy",
            ["CovertBanshee"] = "Covert Banshee",
            ["NovaDefensiveDroneUnit"] = "Defensive Drone",

            // v2 names
            ["DefensiveDroneUnit"] = "Defensive Drone",

            // normalized names
            ["Covert Banshee"] = "Covert Banshee",
            ["Elite Marine"] = "Elite Marine",
            ["Heavy Siege Tank"] = "Heavy Siege Tank",
            ["Hellbat Ranger"] = "Hellbat Ranger",
            ["Holo Decoy"] = "Holo Decoy",
            ["Marauder Commando"] = "Marauder Commando",
            ["Missile Turret"] = "Missile Turret",
            ["Raid Liberator"] = "Raid Liberator",
            ["Railgun Turret"] = "Railgun Turret",
            ["Raven Type-II"] = "Raven Type-II",
            ["Spec Ops Ghost"] = "Spec Ops Ghost",
            ["Strike Goliath"] = "Strike Goliath",

        }.ToFrozenDictionary();

    private static string GetNovaUnitName(string name) =>
        NovaUnitMap.TryGetValue(name, out var mapped) ? mapped : name;
}
