using dsstats.shared;
using dsstats.shared.Units;
using Microsoft.EntityFrameworkCore;

namespace dsstats.dbServices.Builds;

public partial class BuildsService
{
    public async Task<List<DsUnitListDto>> GetUnits(DsUnitsRequest request)
    {
        var units = context.DsUnits.AsQueryable();

        if (request.Commander != Commander.None)
        {
            units = units.Where(x => x.Commander == request.Commander);
        }

        if (!string.IsNullOrEmpty(request.Search))
        {
            units = units.Where(x => x.Name.Contains(request.Search));
        }

        return await units
            .Select(s => new DsUnitListDto()
            {
                DsUnitId = s.DsUnitId,
                Name = s.Name,
                Commander = s.Commander,
                Life = s.Life,
                Cost = s.Cost,
                UnitType = s.UnitType,
            })
            .ToListAsync();
    }

    public async Task<DsUnitDto> GetUnit(int dsUnitId)
    {
        var unit = await context.DsUnits
            .Where(x => x.DsUnitId == dsUnitId)
            .Select(s => new DsUnitDto()
            {
                Name = s.Name,
                Commander = s.Commander,
                Tier = s.Tier,
                Cost = s.Cost,
                Life = s.Life,
                Shields = s.Shields,
                Speed = s.Speed,
                Armor = s.Armor,
                ShieldArmor = s.ShieldArmor,
                StartingEnergy = s.StartingEnergy,
                MaxEnergy = s.MaxEnergy,
                HealthRegen = s.HealthRegen,
                EnergyRegen = s.EnergyRegen,
                UnitType = s.UnitType,
                MovementType = s.MovementType,
                Weapons = s.Weapons.OrderBy(o => o.Name).Select(t => new DsWeaponDto()
                {
                    Name = t.Name,
                    Range = t.Range,
                    AttackSpeed = t.AttackSpeed,
                    Attacks = t.Attacks,
                    CanTarget = t.CanTarget,
                    Damage = t.Damage,
                    DamagePerUpgrade = t.DamagePerUpgrade,
                    BonusDamages = t.BonusDamages.Select(u => new BonusDamageDto()
                    {
                        UnitType = u.UnitType,
                        Damage = u.Damage,
                        PerUpgrade = u.PerUpgrade,
                    }).ToList()
                }).ToList(),
                Abilities = s.Abilities.OrderBy(o => o.Name).Select(v => new DsAbilityDto()
                {
                    Name = v.Name,
                    Commander = v.Commander,
                    Requirements = v.Requirements,
                    Cooldown = v.Cooldown,
                    GlobalTimer = v.GlobalTimer,
                    EnergyCost = v.EnergyCost,
                    CastRange = v.CastRange,
                    AoeRadius = v.AoeRadius,
                    AbilityTarget = v.AbilityTarget,
                    Description = v.Description,
                }).ToList(),
                Upgrades = s.Upgrades.OrderBy(o => o.Upgrade).Select(w => new DsUpgradeDto()
                {
                    Upgrade = w.Upgrade,
                    Commander = w.Commander,
                    Cost = w.Cost,
                    RequiredTier = w.RequiredTier,
                    Description = w.Description,
                }).ToList()
            })
            .FirstOrDefaultAsync();

        return unit ?? new();
    }

    public async Task SetBuildResponseLifeAndCost(BuildsResponse buildResponse, Commander cmdr)
    {
        var dsUnits = await GetUnits(new() { Commander = cmdr });

        foreach (var buildUnit in buildResponse.Units)
        {
            var unitName = MapUnitName(buildUnit.Name, cmdr);
            var dsUnit = dsUnits.FirstOrDefault(f => f.Name.Equals(unitName, StringComparison.Ordinal)
                && f.Commander == cmdr);

            if (dsUnit is null)
            {
                continue;
            }

            buildUnit.Name = unitName;
            buildUnit.Life = Math.Round(dsUnit.Life * buildUnit.Count, 2);
            buildUnit.Cost = Math.Round(dsUnit.Cost * buildUnit.Count, 2);
        }
    }

    private static string MapUnitName(string unitName, Commander cmdr)
    {
        return UnitMap.GetNormalizedUnitName(unitName, cmdr);
    }
}

