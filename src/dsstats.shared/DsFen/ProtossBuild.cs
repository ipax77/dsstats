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
            { "HighTemplar", new('f', 1, true) },
            { "Archon", new('f', 2, true, true) },
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
            { "BlinkTech", new('w') },
            { "AdeptPiercingAttack", new('e') },
            { "ObserverGraviticBooster", new('r') },
            { "PsiStormTech", new('a') },
            { "PhoenixRangeUpgrade", new('s') },
            { "ExtendedThermalLance", new('d') },
            { "VoidRaySpeedUpgrade", new('f') },
            { "DarkTemplarBlinkUpgrade", new('z') },
        };

        UpgradeMap = new Dictionary<string, BuildOption>
        {
            { "ProtossGroundWeaponsLevel1", new('a') },
            { "ProtossGroundWeaponsLevel2", new('a') },
            { "ProtossGroundWeaponsLevel3", new('a') },
            { "ProtossGroundArmorsLevel1", new('s') },
            { "ProtossGroundArmorsLevel2", new('s') },
            { "ProtossGroundArmorsLevel3", new('s') },
            { "ProtossShieldsLevel1", new('d') },
            { "ProtossShieldsLevel2", new('d') },
            { "ProtossShieldsLevel3", new('d') },
            { "ProtossAirWeaponsLevel1", new('f') },
            { "ProtossAirWeaponsLevel2", new('f') },
            { "ProtossAirWeaponsLevel3", new('f') },
            { "ProtossAirArmorsLevel1", new('g') },
            { "ProtossAirArmorsLevel2", new('g') },
            { "ProtossAirArmorsLevel3", new('g') },
        };
        CreateActiveUnits();
    }
}