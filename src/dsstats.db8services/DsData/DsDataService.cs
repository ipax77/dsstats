using AutoMapper;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using dsstats.db8;
using dsstats.shared;
using dsstats.shared.Interfaces;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text.RegularExpressions;

namespace dsstats.db8services.DsData;

public partial class DsDataService(ReplayContext context,
                                   IMapper mapper,
                                   DsUnitRepository dsUnitRepository,
                                   ILogger<DsDataService> logger) : IDsDataService
{
    public void SetUnitIds()
    {

    }


    public void SetUnitColors()
    {
        var units = context.DsUnits
            .Where(x => x.Cost > 0)
            .ToList();

        var groups = units.GroupBy(g => g.Commander);

        foreach (var group in groups)
        {
            int i = 0;
            foreach (var unit in group.OrderBy(o => o.DsUnitId))
            {
                unit.Color = i switch
                {
                    0 => UnitColor.Color1,
                    1 => UnitColor.Color2,
                    2 => UnitColor.Color3,
                    3 => UnitColor.Color4,
                    4 => UnitColor.Color5,
                    5 => UnitColor.Color6,
                    6 => UnitColor.Color7,
                    7 => UnitColor.Color8,
                    8 => UnitColor.Color9,
                    9 => UnitColor.Color10,
                    10 => UnitColor.Color11,
                    11 => UnitColor.Color12,
                    12 => UnitColor.Color13,
                    13 => UnitColor.Color14,
                    14 => UnitColor.Color15,
                    _ => UnitColor.None
                };
                i++;
            }
        }
        context.SaveChanges();
    }

    public void ImportUpgrades()
    {
        var unitsCsv = @"C:\data\ds\DsData\upgrades.csv";
        if (!File.Exists(unitsCsv))
        {
            return;
        }

        List<DsUpgradeDto> upgrades = [];

        try
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
            };

            using var reader = new StreamReader(unitsCsv);
            using var csv = new CsvReader(reader, config);

            csv.Context.RegisterClassMap<DsUpgradeMap>();
            upgrades = csv.GetRecords<DsUpgradeDto>().ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }

        var dbUpgrades = mapper.Map<List<DsUpgrade>>(upgrades.Where(x => !string.IsNullOrEmpty(x.Upgrade)));

        context.DsUpgrades.AddRange(dbUpgrades);
        context.SaveChanges();
    }

    public void ImportUnits()
    {
        // todo:
        // UnitTargetType - Unit can be attaced by Air and/or Ground
        var unitsCsv = @"C:\data\ds\DsData\units.csv";
        if (!File.Exists(unitsCsv))
        {
            return;
        }

        List<DsUnitDto> units = [];

        try
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
            };

            using var reader = new StreamReader(unitsCsv);
            using var csv = new CsvReader(reader, config);

            csv.Context.RegisterClassMap<DsUnitCsvMap>();
            units = csv.GetRecords<DsUnitDto>().ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }

        var dbUnits = mapper.Map<List<DsUnit>>(units.Where(x => !string.IsNullOrEmpty(x.Name)));

        context.DsUnits.AddRange(dbUnits);
        context.SaveChanges();
    }

    public void ImportAbilities()
    {
        var csvFile = @"C:\data\ds\DsData\abilities.csv";

        if (!File.Exists(csvFile))
        {
            return;
        }

        List<DsAbilitsCsvDto> abilities = [];

        try
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
            };

            using var reader = new StreamReader(csvFile);
            using var csv = new CsvReader(reader, config);

            csv.Context.RegisterClassMap<DsAbilityMap>();
            abilities = csv.GetRecords<DsAbilitsCsvDto>().ToList();

            Console.Write(abilities.Count);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }

        var dbAbilities = GetDbAbilities(abilities.Where(x => !string.IsNullOrEmpty(x.Name)).ToList());
        context.DsAbilities.AddRange(dbAbilities);
        context.SaveChanges();
    }

    private List<DsAbility> GetDbAbilities(List<DsAbilitsCsvDto> abilities)
    {
        List<DsAbility> dbAbilites = [];
        var units = context.DsUnits.ToList();

        foreach (var ability in abilities)
        {
            var dbAbility = mapper.Map<DsAbility>(ability);
            var unitNames = ability.UnitName.Split(',', StringSplitOptions.RemoveEmptyEntries);

            foreach (var unitName in unitNames)
            {
                var name = FixAbilityUnitName(unitName, ability.Commander);
                var unit = units.FirstOrDefault(f => f.Name == name && f.Commander == ability.Commander);
                if (unit is null)
                {
                    if (name != "Builder")
                    {
                        logger.LogInformation("Ability unit not found: {name}, {cmdr}", name, ability.Commander);
                    }
                }
                else
                {
                    dbAbility.DsUnits.Add(unit);
                }
            }
            dbAbilites.Add(dbAbility);
        }
        return dbAbilites;
    }

    private string FixAbilityUnitName(string name, Commander cmdr)
    {
        return (name.Trim(), cmdr) switch
        {
            ("Tempest", Commander.Artanis) => "Purifier Tempest",
            ("Warhound Turret", Commander.Mengsk) => "Warhound",
            ("Replenishable Magazine", Commander.Raynor) => "Vulture",
            ("Observer", Commander.Vorazun) => "Oracle",
            ("Kev Rattlesnake West", Commander.Tychus) => "Kev \"Rattlesnake\" West",
            ("Miles Blaze Lewis", Commander.Tychus) => "Miles \"Blaze\" Lewis",
            ("Rob Cannonball Boswell", Commander.Tychus) => "Rob \"Cannonball\" Boswell",
            _ => name.Trim()
        };
    }

    public void QueryTest()
    {
        var goodVsLightUnits = context.DsUnits
            .Where(x => x.Weapons.Any(a => a.BonusDamages.Any(a => a.UnitType == UnitType.Light)))
            .ToList();

        var lightUnits = context.DsUnits.Where(x => x.UnitType.HasFlag(UnitType.Light)).ToList();

        logger.LogWarning("lightUnits: {lightUnits} => units good vs light: {goodVsLightUnits}", lightUnits.Count, goodVsLightUnits.Count);

        var goodVsArmoredUnits = context.DsUnits
            .Where(x => x.Weapons.Any(a => a.BonusDamages.Any(a => a.UnitType == UnitType.Armored)))
            .ToList();

        var armoredUnits = context.DsUnits.Where(x => x.UnitType.HasFlag(UnitType.Armored)).ToList();

        logger.LogWarning("armoredUnits: {armoredUnits} => units good vs armored: {goodVsArmoredUnits}", armoredUnits.Count, goodVsArmoredUnits.Count);
    }

    public void AddZeratulUnits()
    {
        List<DsUnit> units = new()
        {
            new()
            {
                Name = "Zeratul",
                Commander = Commander.Zeratul,
                Cost = 500,
                Life = 150,
                Shields = 450,
                Armor = 1,
                ShieldArmor = 0,
                Tier = 2,
                UnitType = UnitType.Light | UnitType.Biological | UnitType.Psionic | UnitType.Heroic,
                Color = UnitColor.Color1,
                Size = UnitSize.Hero,
                MovementType = WeaponTarget.Ground
            },
            new()
            {
                Name = "Xel'Naga Ambusher",
                Commander = Commander.Zeratul,
                Cost = 250,
                Life = 100,
                Shields = 100,
                Armor = 1,
                ShieldArmor = 0,
                Tier = 2,
                UnitType = UnitType.Armored | UnitType.Mechanical,
                Color = UnitColor.Color2,
                Size = UnitSize.Normal,
                MovementType = WeaponTarget.Ground
            },
            new()
            {
                Name = "Xel'Naga Shieldguard",
                Commander = Commander.Zeratul,
                Cost = 500,
                Life = 120,
                Shields = 120,
                Armor = 1,
                ShieldArmor = 0,
                Tier = 2,
                UnitType = UnitType.Light | UnitType.Mechanical | UnitType.Psionic,
                Color = UnitColor.Color3,
                Size = UnitSize.Small,
                MovementType = WeaponTarget.Ground
            },
            new()
            {
                Name = "Telbrus",
                Commander = Commander.Zeratul,
                Cost = 500,
                Life = 80,
                Shields = 80,
                Armor = 0,
                ShieldArmor = 0,
                StartingEnergy = 300,
                MaxEnergy = 300,
                UnitType = UnitType.Light | UnitType.Biological,
                Color = UnitColor.Color4,
                Size = UnitSize.Small,
                MovementType = WeaponTarget.Ground
            },
            new()
            {
                Name = "Honor Guard",
                Commander = Commander.Zeratul,
                Cost = 80,
                Life = 100,
                Shields = 50,
                Armor = 1,
                ShieldArmor = 0,
                Tier = 2,
                UnitType = UnitType.Light | UnitType.Biological,
                Color = UnitColor.Color4,
                Size = UnitSize.VerySmall,
                MovementType = WeaponTarget.Ground
            },
            new()
            {
                Name = "Void Templar",
                Commander = Commander.Zeratul,
                Cost = 450,
                Life = 80,
                Shields = 320,
                Armor = 1,
                ShieldArmor = 0,
                Tier = 2,
                UnitType = UnitType.Light | UnitType.Biological | UnitType.Psionic,
                Color = UnitColor.Color5,
                Size = UnitSize.Small,
                MovementType = WeaponTarget.Ground
            },
            new()
            {
                Name = "Xel'Naga Abrogator",
                Commander = Commander.Zeratul,
                Cost = 650,
                Life = 200,
                Shields = 200,
                Armor = 1,
                ShieldArmor = 0,
                UnitType = UnitType.Armored | UnitType.Mechanical,
                Color = UnitColor.Color6,
                Size = UnitSize.Big,
                MovementType = WeaponTarget.Ground
            },
            new()
            {
                Name = "Xel'Naga Watcher",
                Commander = Commander.Zeratul,
                Cost = 125,
                Life = 40,
                Shields = 20,
                Armor = 1,
                ShieldArmor = 0,
                Tier = 2,
                UnitType = UnitType.Light | UnitType.Mechanical,
                Color = UnitColor.Color7,
                Size = UnitSize.Small,
                MovementType = WeaponTarget.Air
            },
            new()
            {
                Name = "Xel'Naga Enforcer",
                Commander = Commander.Zeratul,
                Cost = 600,
                Life = 400,
                Shields = 200,
                Armor = 1,
                ShieldArmor = 0,
                Tier = 2,
                UnitType = UnitType.Armored | UnitType.Mechanical,
                Color = UnitColor.Color8,
                Size = UnitSize.Normal,
                MovementType = WeaponTarget.Ground
            }
        };

        context.DsUnits.AddRange(units);
        context.SaveChanges();
    }
}

public class CommanderConverter : DefaultTypeConverter
{
    public override object ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
    {
        if (Enum.TryParse(text?.Trim(), out Commander cmdr))
        {
            return cmdr;
        }
        else if (text?.Equals("HNH", StringComparison.OrdinalIgnoreCase) ?? false)
        {
            return Commander.Horner;
        }
        else if (text?.Equals("Han and Horner", StringComparison.OrdinalIgnoreCase) ?? false)
        {
            return Commander.Horner;
        }
        else
        {
            return Commander.None;
        }
    }
}

public class UnitTypeConverter : DefaultTypeConverter
{
    public override object ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
    {
        if (string.IsNullOrEmpty(text))
        {
            return UnitType.None;
        }

        var ents = text.Split(',', StringSplitOptions.RemoveEmptyEntries);
        if (ents.Length > 0)
        {
            List<UnitType> unitTypes = [];
            for (int i = 0; i < ents.Length; i++)
            {
                if (Enum.TryParse(ents[i].Trim(), out UnitType unitType))
                {
                    unitTypes.Add(unitType);
                }
                else if (ents[i].Trim().Equals("Important Hero", StringComparison.OrdinalIgnoreCase))
                {
                    unitTypes.Add(UnitType.ImportantHero);
                }
            }
            if (unitTypes.Count > 0)
            {
                UnitType unitType = unitTypes[0];
                for (int i = 1; i < unitTypes.Count; i++)
                {
                    unitType |= unitTypes[i];
                }
                return unitType;
            }
        }

        return UnitType.None;
    }
}

public class WeaponTypeConverter : DefaultTypeConverter
{
    public override object ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
    {
        List<DsWeaponDto> weapons =
        [
            new(),
            new(),
            new()
        ];

        SetWeaponProperties(row, 16, weapons[0]);
        SetWeaponProperties(row, 34, weapons[1]);
        SetWeaponProperties(row, 52, weapons[2]);

        return weapons.Where(x => !string.IsNullOrEmpty(x.Name)).ToList();
    }

    private void SetWeaponProperties(IReaderRow row, int startIndex, DsWeaponDto weaponDto)
    {
        weaponDto.Name = row.GetField(startIndex) ?? string.Empty;

        if (string.IsNullOrEmpty(weaponDto.Name))
        {
            return;
        }

        if (int.TryParse(row.GetField(startIndex + 1), out int weapon1Range))
        {
            weaponDto.Range = weapon1Range;
        }
        if (row.TryGetField<float>(startIndex + 2, out float attacSpeed))
        {
            weaponDto.AttackSpeed = attacSpeed;
        }
        if (row.TryGetField<int>(startIndex + 3, out int attacs))
        {
            weaponDto.Attacks = attacs;
        }
        weaponDto.CanTarget = GetWeaponTarget(row.GetField<string>(startIndex + 4));
        if (row.TryGetField<int>(startIndex + 5, out int damage))
        {
            weaponDto.Damage = damage;
        }
        if (row.TryGetField<int>(startIndex + 6, out int damageUp))
        {
            weaponDto.DamagePerUpgrade = damageUp;
        }
        weaponDto.BonusDamages = GetBonusDamage(row, startIndex + 7);
    }

    private List<BonusDamageDto> GetBonusDamage(IReaderRow row, int startindex)
    {
        List<BonusDamageDto> bonusDamages = [new(), new(), new()];

        for (int i = startindex; i < startindex + 11; i += 2)
        {
            if (int.TryParse(row.GetField(i), out int damage))
            {
                UnitType unitType = i switch
                {
                    _ when i == startindex => UnitType.Light,
                    _ when i == startindex + 2 => UnitType.Armored,
                    _ when i == startindex + 4 => UnitType.Biological,
                    _ when i == startindex + 6 => UnitType.Structure,
                    _ when i == startindex + 8 => UnitType.Massive,
                    _ => UnitType.None
                };

                var bonusDamage = i switch
                {
                    _ when i < 39 => bonusDamages[0],
                    _ when i < 54 => bonusDamages[1],
                    _ when i >= 54 => bonusDamages[2],
                    _ => throw new NotImplementedException()
                };


                bonusDamage.UnitType = unitType;
                bonusDamage.Damage = damage;
                if (int.TryParse(row.GetField(i + 1), out int upgrade))
                {
                    bonusDamage.PerUpgrade = upgrade;
                }
            }
        }
        return bonusDamages.Where(x => x.UnitType != UnitType.None).ToList();
    }

    private WeaponTarget GetWeaponTarget(string? ent)
    {
        if (string.IsNullOrEmpty(ent))
        {
            return WeaponTarget.None;
        }

        var ents = ent.Split(',', StringSplitOptions.RemoveEmptyEntries);
        if (ents.Length > 0)
        {
            List<WeaponTarget> targets = [];
            for (int i = 0; i < ents.Length; i++)
            {
                if (Enum.TryParse(ents[i].Trim(), out WeaponTarget target))
                {
                    targets.Add(target);
                }
            }
            if (targets.Count > 0)
            {
                WeaponTarget weaponTarget = targets[0];
                for (int i = 1; i < targets.Count; i++)
                {
                    weaponTarget |= targets[i];
                }
                return weaponTarget;
            }
        }

        return WeaponTarget.None;
    }
}

public class AbilityTargetConverter : DefaultTypeConverter
{
    public override object ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
    {
        if (string.IsNullOrEmpty(text))
        {
            return UnitType.None;
        }

        var ents = text.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (ents.Length > 0)
        {
            List<AbilityTarget> abilityTargets = [];
            for (int i = 0; i < ents.Length; i++)
            {
                if (Enum.TryParse(ents[i].Trim(), true, out AbilityTarget abilityTarget))
                {
                    abilityTargets.Add(abilityTarget);
                }
            }
            if (abilityTargets.Count > 0)
            {
                AbilityTarget abilityTarget = abilityTargets[0];
                for (int i = 1; i < abilityTargets.Count; i++)
                {
                    abilityTarget |= abilityTargets[i];
                }
                return abilityTarget;
            }
        }

        return AbilityTarget.None;
    }
}

public class CustomIntConverter : DefaultTypeConverter
{
    public override object ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
    {
        if (int.TryParse(text, out int value))
        {
            return value;
        }
        else
        {
            return 0;
        }
    }
}

public class GlobalTimerConverter : DefaultTypeConverter
{
    public override object ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
    {
        if (text?.Equals("Yes", StringComparison.OrdinalIgnoreCase) ?? false)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
}

public partial class NameConverter : DefaultTypeConverter
{
    public override object ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        if (text.Contains('(', StringComparison.Ordinal))
        {
            string name = NameRx().Replace(text, "");
            return name.Trim();
        }

        return text.Trim();
    }

    [GeneratedRegex(@"(\(.*?\))")]
    private static partial Regex NameRx();
}

public partial class TierConverter : DefaultTypeConverter
{
    public override object ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        if (text.Contains("T3", StringComparison.OrdinalIgnoreCase))
        {
            return 3;
        }

        if (text.Contains("T2", StringComparison.OrdinalIgnoreCase))
        {
            return 2;
        }

        return 0;
    }
}

public class DsUnitCsvMap : ClassMap<DsUnitDto>
{
    public DsUnitCsvMap()
    {
        Map(m => m.Name).Name("Name").TypeConverter<NameConverter>();
        Map(m => m.Commander).Name("CMDR:").TypeConverter<CommanderConverter>();
        Map(m => m.Tier).Name("Tier").Default(1);
        Map(m => m.Cost).Name("Cost").Default(0);
        Map(m => m.Life).Name("Life").Default(0);
        Map(m => m.Shields).Name("Shields").Default(0); ;
        Map(m => m.Speed).Name("Speed").Default(0); ;
        Map(m => m.Armor).Name("Armor").Default(0); ;
        Map(m => m.ShieldArmor).Name("Shield Armor").Default(0); ;
        Map(m => m.StartingEnergy).Name("Starting Energy:").Default(0);
        Map(m => m.MaxEnergy).Name("Max Energy:").Default(0);
        Map(m => m.HealthRegen).Name("Health Regen:").Default(0); ;
        Map(m => m.EnergyRegen).Name("Energy Regen:").Default(0); ;
        Map(m => m.UnitType).Name("Type:").TypeConverter<UnitTypeConverter>();
        Map(m => m.Weapons).Index(16).TypeConverter<WeaponTypeConverter>();
    }
}

public class DsAbilityMap : ClassMap<DsAbilitsCsvDto>
{
    public DsAbilityMap()
    {
        Map(m => m.Name).Name("Name:");
        Map(m => m.Requirements).Name("Requirements:");
        Map(m => m.Cooldown).Name("Cooldown:").TypeConverter<CustomIntConverter>();
        Map(m => m.GlobalTimer).Name("Global Timer?").TypeConverter<GlobalTimerConverter>();
        Map(m => m.EnergyCost).Name("Energy Cost:").Default(0);
        Map(m => m.CastRange).Name("Cast Range:").TypeConverter<CustomIntConverter>();
        Map(m => m.AoeRadius).Name("AOE Radius").Default(0);
        Map(m => m.AbilityTarget).Name("Target:").TypeConverter<AbilityTargetConverter>();
        Map(m => m.Description).Name("Description:");

        Map(m => m.UnitName).Name("Unit:").TypeConverter<NameConverter>();
        Map(m => m.Commander).Name("CMDR:").TypeConverter<CommanderConverter>();
    }
}

public class DsUpgradeMap : ClassMap<DsUpgradeDto>
{
    public DsUpgradeMap()
    {
        Map(m => m.Upgrade).Name("Upgrade:").TypeConverter<NameConverter>();
        Map(m => m.Commander).Name("Cmdr:").TypeConverter<CommanderConverter>();
        Map(m => m.Cost).Name("Cost:").TypeConverter<CustomIntConverter>();
        Map(m => m.Description).Name("Description:");
        Map(m => m.RequiredTier).Name("Requirement:").TypeConverter<TierConverter>();
    }
}