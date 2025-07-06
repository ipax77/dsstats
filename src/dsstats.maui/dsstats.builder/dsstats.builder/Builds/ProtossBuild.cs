namespace dsstats.builder;

public class ProtossBuild : CmdrBuild
{
    public ProtossBuild()
    {
        UnitMap = new Dictionary<string, char>
        {
            // Q-menu units
            { "Zealot", 'q' },
            { "Stalker", 'w' },
            { "Adept", 'e' },
            { "Sentry", 'r' },
            { "HighTemplar", 't' },
            { "DarkTemplar", 'a' },
            { "Immortal", 's' },
            { "Colossus", 'd' },
            { "Disruptor", 'f' },
            { "Archon", 'g' },
            { "Phoenix", 'z' },
            { "VoidRay", 'x' },
            { "Oracle", 'c' },
            { "Tempest", 'v' },
            { "Carrier", 'b' },
            { "Mothership", 'n' },
        };

        AbilityMap = new Dictionary<string, char>
        {
            // W-menu: abilities and tech
            { "Charge", 'q' },
            { "Blink", 'w' },
            { "ResonatingGlaives", 'e' },
            { "GuardianShield", 'r' },
            { "PsionicStorm", 't' },
            { "ShadowStride", 'a' },
            { "Barrier", 's' },
            { "ExtendedThermalLance", 'd' },
            { "PurificationNova", 'f' },
            { "GravitonBeam", 'g' },
            { "FluxVanes", 'z' },
            { "Revelation", 'x' },
            { "TectonicDestabilizers", 'c' },
            { "InterceptorLaunchSpeed", 'v' },
            { "MassRecall", 'b' },
        };

        UpgradeMap = new Dictionary<string, char>
        {
            // Forge + Cyber Core + Fleet Beacon
            { "GroundWeaponsLevel1", 'a' },
            { "GroundWeaponsLevel2", 'a' },
            { "GroundWeaponsLevel3", 'a' },
            { "GroundArmorLevel1", 's' },
            { "GroundArmorLevel2", 's' },
            { "GroundArmorLevel3", 's' },
            { "ShieldsLevel1", 'd' },
            { "ShieldsLevel2", 'd' },
            { "ShieldsLevel3", 'd' },
            { "AirWeaponsLevel1", 'f' },
            { "AirWeaponsLevel2", 'f' },
            { "AirWeaponsLevel3", 'f' },
            { "AirArmorLevel1", 'g' },
            { "AirArmorLevel2", 'g' },
            { "AirArmorLevel3", 'g' },
        };
    }
}