namespace dsstats.builder;

public class ProtossBuild : CmdrBuild
{
    public ProtossBuild()
    {
        UnitMap = new Dictionary<string, BuildOption>
        {
            // Q-menu units
            { "Zealot", new('q') },
            { "Stalker", new('w') },
            { "Adept", new('e') },
            { "Sentry", new('r') },
            { "HighTemplar", new('t') },
            { "DarkTemplar", new('a') },
            { "Immortal", new('s') },
            { "Colossus", new('d') },
            { "Disruptor", new('f') },
            { "Archon", new('g') },
            { "Phoenix", new('z') },
            { "VoidRay", new('x') },
            { "Oracle", new('c') },
            { "Tempest", new('v') },
            { "Carrier", new('b') },
            { "Mothership", new('n') },
        };

        AbilityMap = new Dictionary<string, BuildOption>
        {
            // W-menu: abilities and tech
            { "Charge", new('q') },
            { "Blink", new('w') },
            { "ResonatingGlaives", new('e') },
            { "GuardianShield", new('r') },
            { "PsionicStorm", new('t') },
            { "ShadowStride", new('a') },
            { "Barrier", new('s') },
            { "ExtendedThermalLance", new('d') },
            { "PurificationNova", new('f') },
            { "GravitonBeam", new('g') },
            { "FluxVanes", new('z') },
            { "Revelation", new('x') },
            { "TectonicDestabilizers", new('c') },
            { "InterceptorLaunchSpeed", new('v') },
            { "MassRecall", new('b') },
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