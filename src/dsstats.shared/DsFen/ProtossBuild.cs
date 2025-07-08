namespace dsstats.shared.DsFen;

public class ProtossBuild : CmdrBuild
{
    public ProtossBuild()
    {
        UnitMap = new Dictionary<string, BuildOption>
        {
            // Q-menu units
            { "Zealot", new('q') },
            { "Stalker", new('w') },
            { "Sentry", new('e') },
            { "Adept", new('r') },
            { "Observer", new('t', true, true, true) },
            { "Oracle", new('t', true, true) },
            { "DarkTemplar", new('a') },
            { "Disruptor", new('s') },
            { "Phenix", new('d', true) },
            { "HighTemplar", new('f', true, true) },
            { "Archon", new('f', true) },
            { "Immortal", new('g') },
            { "VoidRay", new('z', true) },
            { "Colossus", new('x', true) },
            { "Tempest", new('c', true) },
            { "Carrier", new('v', true) },
            { "Mothership", new('t', true, IsAbility: true) },
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