using AutoMapper;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using dsstats.db8;
using dsstats.shared;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace dsstats.db8services.DsData;

public class DsDataService(ReplayContext context, IMapper mapper, ILogger<DsDataService> logger)
{
    public void Import()
    {
        // todo:
        // Commander "HNH" (Horner) Enum parsing
        // UnitType "Important Hero" Enum parsing
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
}

public class CommanderConverter : DefaultTypeConverter
{
    public override object ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
    {
        if (Enum.TryParse(text?.Trim(), out Commander cmdr))
        {
            return cmdr;
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



public class DsUnitCsvMap : ClassMap<DsUnitDto>
{
    public DsUnitCsvMap()
    {
        Map(m => m.Name).Name("Name");
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

