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

public static class BuilderUnitCatalog
{
    private static readonly FrozenDictionary<Commander, CommanderCatalog> Catalogs = CreateCatalogs();

    public static bool IsSupported(Commander commander) => Catalogs.ContainsKey(commander);

    public static IReadOnlyList<BuilderUnitDefinition> GetUnits(Commander commander) =>
        Catalogs.TryGetValue(commander, out var catalog) ? catalog.Units : [];

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
        }

        public IReadOnlyList<BuilderUnitDefinition> Units { get; }
        public FrozenDictionary<string, BuilderUnitDefinition> ByName { get; }
        public FrozenDictionary<char, BuilderUnitDefinition> BySymbol { get; }
    }
}
