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
            { "Hellion", new('t') },
            { "Hellbat", new('a') },
            { "SiegeTank", new('s') },
            { "WidowMine", new('d') },
            { "Cyclone", new('f') },
            { "Thor", new('g') },
            { "Viking", new('z') },
            { "Medivac", new('x') },
            { "Banshee", new('c') },
            { "Raven", new('v') },
            { "Battlecruiser", new('b') },
        };

        AbilityMap = new Dictionary<string, BuildOption>
        {
            // W-menu (upgrades & abilities)
            { "Stimpack", new('q') },
            { "ConcussiveShells", new('w') },
            { "CloakingField", new('e') },
            { "DrillingClaws", new('r') },
            { "InfernalPreigniter", new('t') }, // Hellion upgrade
            { "SmartServos", new('a') },        // Viking/Thor transform speed
            { "HighImpactPayload", new('s') },  // Thor mode toggle
            { "HyperflightRotors", new('d') },  // Banshee speed
            { "AdvancedBallistics", new('f') }, // Raven range
            { "WeaponRefit", new('g') },        // Battlecruiser Yamato
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