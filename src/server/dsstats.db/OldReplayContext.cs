
using dsstats.db.UnitModels;
using dsstats.shared;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace dsstats.db.Old;

public class OldReplayContext : DbContext
{
    public DbSet<OldArcadeReplay> ArcadeReplays { get; set; }
    public DbSet<OldArcadeReplayPlayer> ArcadeReplayDsPlayers { get; set; }
    public DbSet<OldPlayer> Players { get; set; }
    public DbSet<OldReplay> Replays { get; set; }
    public DbSet<OldReplayPlayer> ReplayPlayers { get; set; }
    public DbSet<OldSpawn> Spawns { get; set; }
    public DbSet<OldSpawnUnit> SpawnUnits { get; set; }
    public DbSet<OldUnit> Units { get; set; }
    public DbSet<OldPlayerUpgrade> PlayerUpgrades { get; set; }
    public DbSet<OldUpgrade> Upgrades { get; set; }
    public DbSet<DsUnit> DsUnits { get; set; }
    public DbSet<DsWeapon> DsWeapons { get; set; }
    public DbSet<BonusDamage> BonusDamages { get; set; }
    public DbSet<DsAbility> DsAbilities { get; set; }
    public DbSet<DsUpgrade> DsUpgrades { get; set; }

    public OldReplayContext(DbContextOptions<OldReplayContext> options)
        : base(options)
            {
            }
}

public class OldArcadeReplay
{
    public OldArcadeReplay()
    {
        ArcadeReplayDsPlayers = new HashSet<OldArcadeReplayPlayer>();
    }

    [Key]
    public int ArcadeReplayId { get; set; }
    public int RegionId { get; set; }
    public long BnetBucketId { get; set; }
    public long BnetRecordId { get; set; }
    public GameMode GameMode { get; set; }
    [Precision(0)]
    public DateTime CreatedAt { get; set; }
    public int Duration { get; set; }
    public int PlayerCount { get; set; }
    public bool TournamentEdition { get; set; }
    public int WinnerTeam { get; set; }
    [Precision(0)]
    public DateTime Imported { get; set; }
    [StringLength(64)]
    public string ReplayHash { get; set; } = string.Empty;
    public ICollection<OldArcadeReplayPlayer> ArcadeReplayDsPlayers { get; set; }
}

public class OldArcadeReplayPlayer
{
    [Key]
    public int ArcadeReplayDsPlayerId { get; set; }
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;
    public int SlotNumber { get; set; }
    public int Team { get; set; }
    public int Discriminator { get; set; }
    public PlayerResult PlayerResult { get; set; }
    public int ArcadeReplayId { get; set; }
    public OldArcadeReplay? ArcadeReplay { get; set; }
    public int PlayerId { get; set; }
    public OldPlayer? Player { get; set; }
}

public class OldPlayer
{
    [Key]
    public int PlayerId { get; set; }
    [MaxLength(50)]
    public string Name { get; set; } = null!;
    public int ToonId { get; set; }
    public int RegionId { get; set; }
    public int RealmId { get; set; }
}

public class OldReplay
{
    [Key]
    public int ReplayId { get; set; }
    public string ReplayHash { get; set; } = string.Empty;
    public bool TournamentEdition { get; set; }
    [Precision(0)]
    public DateTime GameTime { get; set; }
    [Precision(0)]
    public DateTime Imported { get; set; }
    public int Duration { get; set; }
    public int WinnerTeam { get; set; }
    public PlayerResult PlayerResult { get; set; }
    public GameMode GameMode { get; set; }
    public int Bunker { get; set; }
    public int Cannon { get; set; }
    public int Maxkillsum { get; set; }
    public byte Playercount { get; set; }
    [MaxLength(4000)]
    public string Middle { get; set; } = null!;
    public virtual ICollection<OldReplayPlayer> ReplayPlayers { get; set; } = [];
}

public class OldReplayPlayer
{
    [Key]
    public int ReplayPlayerId { get; set; }
    [MaxLength(50)]
    public string Name { get; set; } = null!;
    [MaxLength(50)]
    public string? Clan { get; set; }
    public int GamePos { get; set; }
    public int Team { get; set; }
    public PlayerResult PlayerResult { get; set; }
    public int Duration { get; set; }
    public Commander Race { get; set; }
    public Commander OppRace { get; set; }
    public int APM { get; set; }
    public int Income { get; set; }
    public int Army { get; set; }
    public int Kills { get; set; }
    public int UpgradesSpent { get; set; }
    public bool IsUploader { get; set; }
    [MaxLength(300)]
    public string TierUpgrades { get; set; } = null!;
    [MaxLength(300)]
    public string Refineries { get; set; } = null!;
    public int ReplayId { get; set; }
    public virtual OldReplay Replay { get; set; } = null!;
    public int PlayerId { get; set; }
    public virtual OldPlayer Player { get; set; } = null!;
    public virtual ICollection<OldSpawn> Spawns { get; set; } = [];
    public virtual ICollection<OldPlayerUpgrade> Upgrades { get; set; } = [];
}

public class OldSpawn
{
    [Key]
    public int SpawnId { get; set; }
    public int Gameloop { get; set; }
    public Breakpoint Breakpoint { get; set; }
    public int Income { get; set; }
    public int GasCount { get; set; }
    public int ArmyValue { get; set; }
    public int KilledValue { get; set; }
    public int UpgradeSpent { get; set; }
    public int ReplayPlayerId { get; set; }
    public OldReplayPlayer? ReplayPlayer { get; set; }
    public virtual ICollection<OldSpawnUnit> Units { get; set; } = [];
}

public class OldSpawnUnit
{
    [Key]
    public int SpawnUnitId { get; set; }
    public byte Count { get; set; }
    [MaxLength(4000)]
    public string Poss { get; set; } = string.Empty;
    public int UnitId { get; set; }
    public OldUnit? Unit { get; set; }
    public int SpawnId { get; set; }
    public virtual OldSpawn? Spawn { get; set; }

}

public class OldPlayerUpgrade
{
    [Key]
    public int PlayerUpgradeId { get; set; }
    public int Gameloop { get; set; }
    public int UpgradeId { get; set; }
    public OldUpgrade? Upgrade { get; set; }
    public int ReplayPlayerId { get; set; }
    public OldReplayPlayer? ReplayPlayer { get; set; }
}

public class OldUnit
{
    [Key]
    public int UnitId { get; set; }
    [MaxLength(50)]
    public string Name { get; set; } = null!;
}

public class OldUpgrade
{
    [Key]
    public int UpgradeId { get; set; }
    [MaxLength(50)]
    public string Name { get; set; } = null!;
}