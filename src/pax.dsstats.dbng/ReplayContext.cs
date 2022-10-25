using Microsoft.EntityFrameworkCore;

namespace pax.dsstats.dbng;

public class ReplayContext : DbContext
{
    public virtual DbSet<Uploader> Uploaders { get; set; } = null!;
    public virtual DbSet<BattleNetInfo> BattleNetInfos { get; set; } = null!;
    public virtual DbSet<Player> Players { get; set; } = null!;
    public virtual DbSet<Replay> Replays { get; set; } = null!;
    public virtual DbSet<ReplayPlayer> ReplayPlayers { get; set; } = null!;
    public virtual DbSet<PlayerUpgrade> PlayerUpgrades { get; set; } = null!;
    public virtual DbSet<Spawn> Spawns { get; set; } = null!;
    public virtual DbSet<SpawnUnit> SpawnUnits { get; set; } = null!;
    public virtual DbSet<Unit> Units { get; set; } = null!;
    public virtual DbSet<Upgrade> Upgrades { get; set; } = null!;
    public virtual DbSet<ReplayEvent> ReplayEvents { get; set; } = null!;
    public virtual DbSet<Event> Events { get; set; } = null!;
    public virtual DbSet<ReplayViewCount> ReplayViewCounts { get; set; } = null!;
    public virtual DbSet<ReplayDownloadCount> ReplayDownloadCounts { get; set; } = null!;

    public ReplayContext(DbContextOptions<ReplayContext> options)
    : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Replay>(entity =>
        {
            entity.HasIndex(e => e.FileName);
            entity.HasIndex(e => new { e.GameTime, e.GameMode });
            entity.HasIndex(e => new { e.GameTime, e.GameMode, e.DefaultFilter });

            entity.Property(p => p.ReplayHash)
                .HasMaxLength(64)
                .IsFixedLength();

            entity.HasIndex(e => e.ReplayHash)
                .IsUnique();

        });

        modelBuilder.Entity<ReplayPlayer>(entity =>
        {
            entity.HasIndex(e => e.Race);
            entity.HasIndex(e => new { e.Race, e.OppRace });

        });

        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasIndex(e => e.Name).IsUnique();
        });

        modelBuilder.Entity<Player>(entity =>
        {
            entity.HasIndex(e => e.ToonId).IsUnique();
        });

        modelBuilder.Entity<Uploader>(entity =>
        {
            entity.HasIndex(e => e.AppGuid).IsUnique();
        });

        modelBuilder.Entity<Unit>(entity =>
        {
            entity.HasIndex(e => new { e.Name, e.Commander }).IsUnique();
        });

        modelBuilder.Entity<Upgrade>(entity =>
        {
            entity.HasIndex(e => e.Name).IsUnique();
        });
    }
}
