namespace dsstats.shared.DsFen;


public class TerranBuild : CmdrBuild
{
    public TerranBuild()
    {
        UnitMap = new Dictionary<string, BuildOption>
        {
            // Q-menu
            { "Marine", new('q') },
            { "Marauder", new('w') },
            { "Reaper", new('e') },
            { "Ghost", new('r') },
            { "Hellion", new('t', 2, false, true, true) },
            { "Hellbat", new('t', 2, false, true) },
            { "Medivac", new('a', 2, true) },
            { "Banshee", new('s', 2) },
            { "Viking", new('d', 2, true, true, true) },
            { "VikingLanded", new('d', 2, false, true) },
            { "Raven", new('f', 2, true) },
            { "SiegeTank", new('g', 2) },
            { "Cyclone", new('z', 2, false, true, true) },
            { "WidowMine", new('z', 1, false, true, false) },
            { "Liberator", new('x', 2, true) },
            { "Thor", new('c', 3, true, true) },
            { "ThorExplosive", new('c', 2, true) },
            { "Battlecruiser", new('v', 3, true) },
        };

        AbilityMap = new Dictionary<string, BuildOption>
        {
            // W-menu (upgrades & abilities)
            { "ShieldWall", new('q') },
            { "PunisherGrenades", new('w') },
            { "Stimpack", new('e') },
            { "PersonalCloaking", new('r') },
            { "HighCapacityBarrels", new('a') },
            { "MedivacCaduceusReactor", new('s') },
            { "MedivacIncreaseSpeedBoost", new('s') },
            { "BansheeCloak", new('d') },
            { "BansheeSpeed", new('f') },
            { "HiSecAutoTracking", new('g') },
            { "CycloneLockOnDamageUpgrade", new('z') },
            { "DrillClaws", new('x') },
            { "LiberatorAGRangeUpgrade", new('c') },
            { "BattlecruiserEnableSpecializations", new('v') },
        };

        UpgradeMap = new Dictionary<string, BuildOption>
        {
            // Main armory upgrades
            { "TerranInfantryWeaponsLevel1", new('a') },
            { "TerranInfantryWeaponsLevel2", new('a') },
            { "TerranInfantryWeaponsLevel3", new('a') },
            { "TerranInfantryArmorsLevel1", new('s') },
            { "TerranInfantryArmorsLevel2", new('s') },
            { "TerranInfantryArmorsLevel3", new('s') },
            { "TerranVehicleWeaponsLevel1", new('d') },
            { "TerranVehicleWeaponsLevel2", new('d') },
            { "TerranVehicleWeaponsLevel3", new('d') },
            { "TerranVehicleAndShipArmorsLevel1", new('f') },
            { "TerranVehicleAndShipArmorsLevel2", new('f') },
            { "TerranVehicleAndShipArmorsLevel3", new('f') },
            { "TerranShipWeaponsLevel1", new('g') },
            { "TerranShipWeaponsLevel2", new('g') },
            { "TerranShipWeaponsLevel3", new('g') },
        };
        CreateActiveUnits();
    }
}