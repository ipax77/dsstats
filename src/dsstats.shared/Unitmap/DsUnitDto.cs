namespace dsstats.shared;

public record DsUnitDto
{
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
    public WeaponTarget MovementType { get; set; }
    public UnitSize Size { get; set; }
    public UnitColor Color { get; set; }
    public int UnitId { get; set; }
    public List<DsWeaponDto> Weapons { get; set; } = [];
    public List<DsAbilityDto> Abilities { get; set; } = []; 
    public List<DsUpgradeDto> Upgrades { get; set; } = [];
}

public record DsUnitListDto
{
    public string Name { get; set; } = string.Empty;
    public Commander Commander { get; set; }
    public int Tier { get; set; }
    public int Cost { get; set; }
    public UnitType UnitType { get; set; }
}

public record DsAbilityDto
{
    public string Name { get; set; } = string.Empty;
    public Commander Commander { get; set; }
    public string Requirements { get; set; } = string.Empty;
    public int Cooldown { get; set; }
    public bool GlobalTimer { get; set; }
    public float EnergyCost { get; set; }
    public int CastRange { get; set; }
    public float AoeRadius { get; set; }
    public AbilityTarget AbilityTarget { get; set; }
    public string Description { get; set; } = string.Empty;
}

public record DsAbilitsCsvDto : DsAbilityDto
{
    public string UnitName {  get; set; } = string.Empty;
}

public record DsWeaponDto
{
    public string Name { get; set; } = string.Empty;
    public float Range { get; set; }
    public float AttackSpeed { get; set; }
    public int Attacks { get; set; }
    public WeaponTarget CanTarget { get; set; }
    public int Damage { get; set; }
    public int DamagePerUpgrade { get; set; }
    public List<BonusDamageDto> BonusDamages { get; set; } = [];
}

public record BonusDamageDto
{
    public UnitType UnitType { get; set; }
    public int Damage { get; set; }
    public int PerUpgrade { get; set; }
}

public record DsUpgradeDto
{
    public string Upgrade { get; set; } = string.Empty;
    public Commander Commander { get; set; }
    public int Cost { get; set; }
    public int RequiredTier { get; set; }
    public string Description { get; set; } = string.Empty;
}