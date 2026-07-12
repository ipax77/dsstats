using System.Collections.Frozen;
using dsstats.shared.Units;

namespace dsstats.shared.Builder;

public sealed record BuilderUnitDefinition(
    string Name,
    char Symbol,
    char BuildKey,
    byte Footprint,
    bool IsAir = false,
    bool RequiresToggle = false,
    bool IsDefaultToggleState = true,
    bool IsAbility = false);

public sealed record BuilderUpgradeDefinition(string Name, char Symbol, char BuildKey, bool IsAbility);

public static class BuilderUnitCatalog
{
    private static readonly FrozenDictionary<Commander, CommanderCatalog> Catalogs = CreateCatalogs();

    public static bool IsSupported(Commander commander) => Catalogs.ContainsKey(commander);

    public static IReadOnlyList<BuilderUnitDefinition> GetUnits(Commander commander) =>
        Catalogs.TryGetValue(commander, out var catalog) ? catalog.Units : [];

    public static IReadOnlyList<BuilderUpgradeDefinition> GetUpgrades(Commander commander) =>
        Catalogs.TryGetValue(commander, out var catalog) ? catalog.Upgrades : [];

    public static bool TryGetUnit(Commander commander, string name, out BuilderUnitDefinition definition)
    {
        if (Catalogs.TryGetValue(commander, out var catalog))
        {
            if (catalog.ByName.TryGetValue(name, out definition!))
            {
                return true;
            }

            var normalized = UnitMap.GetNormalizedUnitName(name, commander);
            if (catalog.ByName.TryGetValue(normalized, out definition!))
            {
                return true;
            }
        }

        definition = null!;
        return false;
    }

    public static bool TryGetUnit(Commander commander, char symbol, out BuilderUnitDefinition definition)
    {
        if (Catalogs.TryGetValue(commander, out var catalog)
            && catalog.BySymbol.TryGetValue(symbol, out definition!))
        {
            return true;
        }

        definition = null!;
        return false;
    }

    public static bool TryGetUpgrade(Commander commander, string name, out BuilderUpgradeDefinition definition)
    {
        if (Catalogs.TryGetValue(commander, out var catalog)
            && catalog.UpgradesByName.TryGetValue(name, out definition!))
        {
            return true;
        }
        definition = null!;
        return false;
    }

    public static bool TryGetUpgrade(Commander commander, char symbol, out BuilderUpgradeDefinition definition)
    {
        if (Catalogs.TryGetValue(commander, out var catalog)
            && catalog.UpgradesBySymbol.TryGetValue(symbol, out definition!))
        {
            return true;
        }
        definition = null!;
        return false;
    }

    private static FrozenDictionary<Commander, CommanderCatalog> CreateCatalogs()
    {
        Dictionary<Commander, CommanderCatalog> catalogs = [];
        catalogs[Commander.Protoss] = Create(
            new("Zealot", 'a', 'q', 1),
            new("Stalker", 'b', 'w', 2),
            new("Sentry", 'c', 'e', 1),
            new("Adept", 'd', 'r', 1),
            new("Observer", 'e', 't', 1, IsAir: true, RequiresToggle: true),
            new("Oracle", 'f', 't', 2, IsAir: true, RequiresToggle: true, IsDefaultToggleState: false),
            new("Dark Templar", 'g', 'a', 1),
            new("Disruptor", 'h', 's', 2),
            new("Phoenix", 'i', 'd', 2, IsAir: true),
            new("High Templar", 'j', 'f', 1),
            new("Archon", 'k', 'f', 2, RequiresToggle: true, IsDefaultToggleState: false),
            new("Immortal", 'l', 'g', 2),
            new("Void Ray", 'm', 'z', 2, IsAir: true),
            new("Colossus", 'n', 'x', 2),
            new("Tempest", 'o', 'c', 3, IsAir: true),
            new("Carrier", 'p', 'v', 3, IsAir: true),
            new("Mothership", 'q', 't', 3, IsAir: true, IsAbility: true));

        catalogs[Commander.Terran] = Create(
            new("Marine", 'a', 'q', 1),
            new("Marauder", 'b', 'w', 1),
            new("Reaper", 'c', 'e', 1),
            new("Ghost", 'd', 'r', 1),
            new("Hellion", 'e', 't', 2, RequiresToggle: true),
            new("Hellbat", 'f', 't', 2, RequiresToggle: true, IsDefaultToggleState: false),
            new("Medivac", 'g', 'a', 2, IsAir: true),
            new("Banshee", 'h', 's', 2, IsAir: true),
            new("Viking", 'i', 'd', 2, IsAir: true, RequiresToggle: true),
            new("Raven", 'j', 'f', 2, IsAir: true),
            new("Siege Tank", 'k', 'g', 2),
            new("Cyclone", 'l', 'z', 2, RequiresToggle: true),
            new("Widow Mine", 'm', 'z', 1, RequiresToggle: true, IsDefaultToggleState: false),
            new("Liberator", 'n', 'x', 2, IsAir: true),
            new("Thor", 'o', 'c', 3, RequiresToggle: true),
            new("Battlecruiser", 'p', 'v', 3, IsAir: true),
            new("ThorAP", 'q', 'c', 3, RequiresToggle: true, IsDefaultToggleState: false),
            new("VikingAssault", 'r', 'd', 2, RequiresToggle: true, IsDefaultToggleState: false));

        catalogs[Commander.Zerg] = Create(
            new("Zergling", 'a', 'q', 1),
            new("Baneling", 'b', 'w', 1),
            new("Roach", 'c', 'e', 1),
            new("Queen", 'd', 'r', 2),
            new("Overseer", 'e', 't', 2, IsAir: true),
            new("Hydralisk", 'f', 'a', 1, RequiresToggle: true),
            new("Lurker", 'g', 'a', 2, RequiresToggle: true, IsDefaultToggleState: false),
            new("Mutalisk", 'h', 's', 1, IsAir: true),
            new("Corruptor", 'i', 'd', 2, IsAir: true),
            new("Infestor", 'j', 'f', 2),
            new("Swarm Host", 'k', 'g', 2),
            new("Viper", 'l', 'z', 2, IsAir: true),
            new("Ultralisk", 'm', 'x', 2),
            new("Brood Lord", 'n', 'c', 2, IsAir: true));

        catalogs[Commander.Protoss].SetUpgrades(
            new("Charge", 'a', 'q', true),
            new("BlinkTech", 'b', 'w', true),
            new("AdeptPiercingAttack", 'c', 'e', true),
            new("ObserverGraviticBooster", 'd', 'r', true),
            new("PsiStormTech", 'e', 'a', true),
            new("PhoenixRangeUpgrade", 'f', 's', true),
            new("ExtendedThermalLance", 'g', 'd', true),
            new("VoidRaySpeedUpgrade", 'h', 'f', true),
            new("DarkTemplarBlinkUpgrade", 'i', 'z', true),
            new("ProtossGroundWeaponsLevel1", 'j', 'a', false),
            new("ProtossGroundWeaponsLevel2", 'k', 'a', false),
            new("ProtossGroundWeaponsLevel3", 'l', 'a', false),
            new("ProtossGroundArmorsLevel1", 'm', 's', false),
            new("ProtossGroundArmorsLevel2", 'n', 's', false),
            new("ProtossGroundArmorsLevel3", 'o', 's', false),
            new("ProtossShieldsLevel1", 'p', 'd', false),
            new("ProtossShieldsLevel2", 'q', 'd', false),
            new("ProtossShieldsLevel3", 'r', 'd', false),
            new("ProtossAirWeaponsLevel1", 's', 'f', false),
            new("ProtossAirWeaponsLevel2", 't', 'f', false),
            new("ProtossAirWeaponsLevel3", 'u', 'f', false),
            new("ProtossAirArmorsLevel1", 'v', 'g', false),
            new("ProtossAirArmorsLevel2", 'w', 'g', false),
            new("ProtossAirArmorsLevel3", 'x', 'g', false));

        catalogs[Commander.Terran].SetUpgrades(
            new("ShieldWall", 'a', 'q', true),
            new("PunisherGrenades", 'b', 'w', true),
            new("Stimpack", 'c', 'e', true),
            new("PersonalCloaking", 'd', 'r', true),
            new("HighCapacityBarrels", 'e', 'a', true),
            new("MedivacCaduceusReactor", 'f', 's', true),
            new("MedivacIncreaseSpeedBoost", 'g', 's', true),
            new("BansheeCloak", 'h', 'd', true),
            new("BansheeSpeed", 'i', 'f', true),
            new("HiSecAutoTracking", 'j', 'g', true),
            new("CycloneLockOnDamageUpgrade", 'k', 'z', true),
            new("DrillClaws", 'l', 'x', true),
            new("LiberatorAGRangeUpgrade", 'm', 'c', true),
            new("BattlecruiserEnableSpecializations", 'n', 'v', true),
            new("TerranInfantryWeaponsLevel1", 'o', 'a', false),
            new("TerranInfantryWeaponsLevel2", 'p', 'a', false),
            new("TerranInfantryWeaponsLevel3", 'q', 'a', false),
            new("TerranInfantryArmorsLevel1", 'r', 's', false),
            new("TerranInfantryArmorsLevel2", 's', 's', false),
            new("TerranInfantryArmorsLevel3", 't', 's', false),
            new("TerranVehicleWeaponsLevel1", 'u', 'd', false),
            new("TerranVehicleWeaponsLevel2", 'v', 'd', false),
            new("TerranVehicleWeaponsLevel3", 'w', 'd', false),
            new("TerranVehicleAndShipArmorsLevel1", 'x', 'f', false),
            new("TerranVehicleAndShipArmorsLevel2", 'y', 'f', false),
            new("TerranVehicleAndShipArmorsLevel3", 'z', 'f', false),
            new("TerranShipWeaponsLevel1", 'A', 'g', false),
            new("TerranShipWeaponsLevel2", 'B', 'g', false),
            new("TerranShipWeaponsLevel3", 'C', 'g', false));

        catalogs[Commander.Zerg].SetUpgrades(
            new("zerglingmovementspeed", 'a', 'q', true),
            new("zerglingattackspeed", 'b', 'w', true),
            new("CentrifugalHooks", 'c', 'e', true),
            new("GlialReconstitution", 'd', 'r', true),
            new("TunnelingClaws", 'e', 't', true),
            new("EvolveGroovedSpines", 'f', 'a', true),
            new("LurkerRange", 'g', 's', true),
            new("DiggingClaws", 'h', 'd', true),
            new("NeuralParasite", 'i', 'g', true),
            new("ChitinousPlating", 'j', 'z', true),
            new("AnabolicSynthesis", 'k', 'x', true),
            new("MuscularAugments", 'l', 'c', true),
            new("overlordspeed", 'm', 'v', true),
            new("ZergMeleeWeaponsLevel1", 'n', 'a', false),
            new("ZergMeleeWeaponsLevel2", 'o', 'a', false),
            new("ZergMeleeWeaponsLevel3", 'p', 'a', false),
            new("ZergGroundArmorsLevel1", 'q', 's', false),
            new("ZergGroundArmorsLevel2", 'r', 's', false),
            new("ZergGroundArmorsLevel3", 's', 's', false),
            new("ZergMissileWeaponsLevel1", 't', 'd', false),
            new("ZergMissileWeaponsLevel2", 'u', 'd', false),
            new("ZergMissileWeaponsLevel3", 'v', 'd', false),
            new("ZergFlyerWeaponsLevel1", 'w', 'f', false),
            new("ZergFlyerWeaponsLevel2", 'x', 'f', false),
            new("ZergFlyerWeaponsLevel3", 'y', 'f', false),
            new("ZergFlyerArmorsLevel1", 'z', 'g', false),
            new("ZergFlyerArmorsLevel2", 'A', 'g', false),
            new("ZergFlyerArmorsLevel3", 'B', 'g', false));

        return catalogs.ToFrozenDictionary();
    }

    private static CommanderCatalog Create(params BuilderUnitDefinition[] units) => new(units);

    private sealed class CommanderCatalog
    {
        public CommanderCatalog(BuilderUnitDefinition[] units)
        {
            Units = units;
            ByName = units.ToFrozenDictionary(unit => unit.Name, StringComparer.OrdinalIgnoreCase);
            BySymbol = units.ToFrozenDictionary(unit => unit.Symbol);
            UpgradesByName = FrozenDictionary<string, BuilderUpgradeDefinition>.Empty;
            UpgradesBySymbol = FrozenDictionary<char, BuilderUpgradeDefinition>.Empty;
        }

        public IReadOnlyList<BuilderUnitDefinition> Units { get; }
        public FrozenDictionary<string, BuilderUnitDefinition> ByName { get; }
        public FrozenDictionary<char, BuilderUnitDefinition> BySymbol { get; }
        public FrozenDictionary<string, BuilderUpgradeDefinition> UpgradesByName { get; private set; }
        public FrozenDictionary<char, BuilderUpgradeDefinition> UpgradesBySymbol { get; private set; }

        public void SetUpgrades(params BuilderUpgradeDefinition[] upgrades)
        {
            Upgrades = upgrades;
            UpgradesByName = upgrades.ToFrozenDictionary(upgrade => upgrade.Name, StringComparer.OrdinalIgnoreCase);
            UpgradesBySymbol = upgrades.ToFrozenDictionary(upgrade => upgrade.Symbol);
        }

        public IReadOnlyList<BuilderUpgradeDefinition> Upgrades { get; private set; } = [];
    }
}
