using dsstats.shared;
using System.ComponentModel.DataAnnotations;

namespace dsstats.db.UnitModels;

public class DsUnit
{
    public int DsUnitId { get; set; }
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    public Commander Commander { get; set; }
    public int Tier { get; set; }
    public int Cost { get; set; }
    public int Life { get; set; }
    public int Shields { get; set; }
    public float Speed { get; set; }
    public int Armor { get; set; }
    public int ShieldArmor { get; set; }
    public int StartingEnergy { get; set; }
    public int MaxEnergy { get; set; }
    public float HealthRegen { get; set; }
    public float EnergyRegen { get; set; }
    public DsUnitType UnitType { get; set; }
    public UnitSize UnitSize { get; set; }
    public UnitType MapUnitType { get; set; }
    public WeaponTarget MovementType { get; set; }
    public ICollection<DsWeapon> Weapons { get; set; } = [];
    public ICollection<DsAbility> Abilities { get; set; } = [];
    public ICollection<DsUpgrade> Upgrades { get; set; } = [];
}

public class DsAbility
{
    public int DsAbilityId { get; set; }
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    public Commander Commander { get; set; }
    [MaxLength(100)]
    public string Requirements { get; set; } = string.Empty;
    public int Cooldown { get; set; }
    public bool GlobalTimer { get; set; }
    public float EnergyCost { get; set; }
    public int CastRange { get; set; }
    public float AoeRadius { get; set; }
    public AbilityTarget AbilityTarget { get; set; }
    [MaxLength(310)]
    public string Description { get; set; } = string.Empty;
    public ICollection<DsUnit> DsUnits { get; set; } = [];
}


public class DsWeapon
{
    public int DsWeaponId { get; set; }
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    public float Range { get; set; }
    public float AttackSpeed { get; set; }
    public int Attacks { get; set; }
    public WeaponTarget CanTarget { get; set; }
    public int Damage { get; set; }
    public int DamagePerUpgrade { get; set; }
    public ICollection<BonusDamage> BonusDamages { get; set; } = [];
    public int DsUnitId { get; set; }
    public DsUnit? DsUnit { get; set; }
}

public class BonusDamage
{
    public int BonusDamageId { get; set; }
    public DsUnitType UnitType { get; set; }
    public int Damage { get; set; }
    public int PerUpgrade { get; set; }
    public int DsWeaponId { get; set; }
    public DsWeapon? DsWeapon { get; set; }
}

public class DsUpgrade
{
    public int DsUpgradeId { get; set; }
    [MaxLength(100)]
    public string Upgrade { get; set; } = string.Empty;
    public Commander Commander { get; set; }
    public int Cost { get; set; }
    public int RequiredTier { get; set; }
    [MaxLength(300)]
    public string Description { get; set; } = string.Empty;
}
