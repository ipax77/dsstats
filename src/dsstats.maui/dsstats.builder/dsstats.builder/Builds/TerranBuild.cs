namespace dsstats.builder;

public class TerranBuild : CmdrBuild
{
    public TerranBuild()
    {
        UnitMap = new Dictionary<string, char>
        {
            // Q-menu
            { "Marine", 'q' },
            { "Marauder", 'w' },
            { "Reaper", 'e' },
            { "Ghost", 'r' },
            { "Hellion", 't' },
            { "Hellbat", 'a' },
            { "SiegeTank", 's' },
            { "WidowMine", 'd' },
            { "Cyclone", 'f' },
            { "Thor", 'g' },
            { "Viking", 'z' },
            { "Medivac", 'x' },
            { "Banshee", 'c' },
            { "Raven", 'v' },
            { "Battlecruiser", 'b' },
        };

        AbilityMap = new Dictionary<string, char>
        {
            // W-menu (upgrades & abilities)
            { "Stimpack", 'q' },
            { "ConcussiveShells", 'w' },
            { "CloakingField", 'e' },
            { "DrillingClaws", 'r' },
            { "InfernalPreigniter", 't' }, // Hellion upgrade
            { "SmartServos", 'a' },        // Viking/Thor transform speed
            { "HighImpactPayload", 's' },  // Thor mode toggle
            { "HyperflightRotors", 'd' },  // Banshee speed
            { "AdvancedBallistics", 'f' }, // Raven range
            { "WeaponRefit", 'g' },        // Battlecruiser Yamato
        };

        UpgradeMap = new Dictionary<string, char>
        {
            // Main armory upgrades
            { "InfantryWeaponsLevel1", 'a' },
            { "InfantryWeaponsLevel2", 'a' },
            { "InfantryWeaponsLevel3", 'a' },
            { "InfantryArmorLevel1", 's' },
            { "InfantryArmorLevel2", 's' },
            { "InfantryArmorLevel3", 's' },
            { "VehicleWeaponsLevel1", 'd' },
            { "VehicleWeaponsLevel2", 'd' },
            { "VehicleWeaponsLevel3", 'd' },
            { "ShipWeaponsLevel1", 'f' },
            { "ShipWeaponsLevel2", 'f' },
            { "ShipWeaponsLevel3", 'f' },
            { "VehiclePlatingLevel1", 'g' },
            { "VehiclePlatingLevel2", 'g' },
            { "VehiclePlatingLevel3", 'g' },
            { "ShipPlatingLevel1", 'h' },
            { "ShipPlatingLevel2", 'h' },
            { "ShipPlatingLevel3", 'h' },
        };
    }
}