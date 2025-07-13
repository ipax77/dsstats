namespace dsstats.shared.DsFen;

public class ProtossBuild : CmdrBuild
{
    public ProtossBuild()
    {
        UnitMap = new Dictionary<string, BuildOption>
        {
            // Q-menu units
            { "Zealot", new('q') },
            { "Stalker", new('w', 2) },
            { "Sentry", new('e') },
            { "Adept", new('r') },
            { "Observer", new('t', 1, true, true, true) },
            { "Oracle", new('t', 2, true, true) },
            { "DarkTemplar", new('a') },
            { "Disruptor", new('s', 2) },
            { "Phenix", new('d', 2, true) },
            { "HighTemplar", new('f', 1, true, true) },
            { "Archon", new('f', 2, true) },
            { "Immortal", new('g', 2) },
            { "VoidRay", new('z', 2, true) },
            { "Colossus", new('x', 2, true) },
            { "Tempest", new('c', 3, true) },
            { "Carrier", new('v', 3, true) },
            { "Mothership", new('t', 3, true, IsAbility: true) },
        };

        AbilityMap = new Dictionary<string, BuildOption>
        {
            // W-menu: abilities and tech
            { "Charge", new('q') },
            { "Blink", new('w') },
            { "ResonatingGlaives", new('e') },
            { "GraviticBoosters", new('r') },
            { "PsionicStorm", new('a') },
            { "AnionPulse-Crystals", new('s') },
            { "ExtendedThermalLance", new('d') },
            { "FluxVanes", new('f') },
            { "ShadowStride", new('z') },
        };

        UpgradeMap = new Dictionary<string, BuildOption>
        {
            // Forge + Cyber Core + Fleet Beacon
            { "GroundWeaponsLevel1", new('a') },
            { "GroundWeaponsLevel2", new('a') },
            { "GroundWeaponsLevel3", new('a') },
            { "GroundArmorLevel1", new('s') },
            { "GroundArmorLevel2", new('s') },
            { "GroundArmorLevel3", new('s') },
            { "ShieldsLevel1", new('d') },
            { "ShieldsLevel2", new('d') },
            { "ShieldsLevel3", new('d') },
            { "AirWeaponsLevel1", new('f') },
            { "AirWeaponsLevel2", new('f') },
            { "AirWeaponsLevel3", new('f') },
            { "AirArmorLevel1", new('g') },
            { "AirArmorLevel2", new('g') },
            { "AirArmorLevel3", new('g') },
        };
        CreateActiveUnits();
    }
}