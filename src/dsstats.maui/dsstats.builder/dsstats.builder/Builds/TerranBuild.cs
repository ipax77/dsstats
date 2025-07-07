namespace dsstats.builder;

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
            { "Hellion", new('t', true, true) },
            { "Hellbat", new('t', true) },
            { "Medivac", new('a') },
            { "Banshee", new('s') },
            { "Viking", new('d', true, true) },
            { "VikingLanded", new('d', true) },
            { "Raven", new('f') },
            { "Tank", new('g') },
            { "Cyclone", new('z', true, true) },
            { "WidowMine", new('z', true) },
            { "Liberator", new('x') },
            { "Thor", new('c', true, true) },
            { "ThorExplosive", new('c', true, true) },
            { "Battlecruiser", new('v') },
        };

        AbilityMap = new Dictionary<string, BuildOption>
        {
            // W-menu (upgrades & abilities)
            { "CombatShield", new('q') },
            { "ConcussiveShells", new('w') },
            { "Stimpack", new('e') },
            { "PersonalCloaking", new('r') },
            { "InfernalPre-Igniter", new('a') },
            { "CaduceusReactor", new('s') },
            { "CloakingField", new('d') },
            { "SmartServos", new('f') },
            { "Mag-FieldAccelerator", new('z') },
            { "DrillingClaws", new('x') },
            { "AdvancedBallistics", new('c') },     
            { "WeaponRefit", new('v') },     
        };

        UpgradeMap = new Dictionary<string, BuildOption>
        {
            // Main armory upgrades
            { "InfantryWeaponsLevel1", new('a') },
            { "InfantryWeaponsLevel2", new('a') },
            { "InfantryWeaponsLevel3", new('a') },
            { "InfantryArmorLevel1", new('s') },
            { "InfantryArmorLevel2", new('s') },
            { "InfantryArmorLevel3", new('s') },
            { "VehicleWeaponsLevel1", new('d') },
            { "VehicleWeaponsLevel2", new('d') },
            { "VehicleWeaponsLevel3", new('d') },
            { "ShipWeaponsLevel1", new('f') },
            { "ShipWeaponsLevel2", new('f') },
            { "ShipWeaponsLevel3", new('f') },
            { "VehiclePlatingLevel1", new('g') },
            { "VehiclePlatingLevel2", new('g') },
            { "VehiclePlatingLevel3", new('g') },
            { "ShipPlatingLevel1", new('h') },
            { "ShipPlatingLevel2", new('h') },
            { "ShipPlatingLevel3", new('h') },
        };
        CreateActiveUnits();
    }
}