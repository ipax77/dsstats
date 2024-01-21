using dsstats.shared;
using System.ComponentModel.DataAnnotations;

namespace dsstats.db8;

public class DsUnit
{
    public DsUnit()
    {
        Weapons = new HashSet<DsWeapon>();
    }

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
    public UnitType UnitType { get; set; }
    public ICollection<DsWeapon> Weapons { get; set; }
}

public class DsWeapon
{
    public DsWeapon()
    {
        BonusDamages = new HashSet<BonusDamage>();
    }
    public int DsWeaponId { get; set; }
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    public float Range { get; set; }
    public float AttackSpeed { get; set; }
    public int Attacks { get; set; }
    public WeaponTarget CanTarget { get; set; }
    public int Damage { get; set; }
    public int DamagePerUpgrade { get; set; }
    public ICollection<BonusDamage> BonusDamages { get; set; }
}

public class BonusDamage
{
    public int BonusDamageId { get; set; }
    public UnitType UnitType { get; set; }
    public int Damage { get; set; }
    public int PerUpgrade { get; set; }
}