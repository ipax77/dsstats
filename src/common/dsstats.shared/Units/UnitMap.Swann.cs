using System.Collections.Frozen;

namespace dsstats.shared.Units;

public static partial class UnitMap
{
    private static readonly FrozenDictionary<string, string> SwannUnitMap =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // v3 names
            ["HellbatSwann"] = "Hellbat",
            ["ScienceVesselStarlight"] = "Science Vessel",
            ["SwannCyclone"] = "Cyclone",
            ["GoliathStarlight"] = "Goliath",
            ["SwannThor"] = "Thor",
            ["ARES"] = "A.R.E.S.",
            ["WraithStarlight"] = "Wraith",

            // v2 names
            ["ScienceVessel"] = "Science Vessel",

            // normalized names
            ["A.R.E.S."] = "A.R.E.S.",
            ["Devastation Turret"] = "Devastation Turret",
            ["Drakken Laser Drill"] = "Drakken Laser Drill",
            ["Missile Turret"] = "Missile Turret",
            ["Science Vessel"] = "Science Vessel",
            ["Siege Tank"] = "Siege Tank",

        }.ToFrozenDictionary();

    private static string GetSwannUnitName(string name) =>
        SwannUnitMap.TryGetValue(name, out var mapped) ? mapped : name;
}
