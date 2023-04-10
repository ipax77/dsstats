using Microsoft.EntityFrameworkCore;

namespace pax.dsstats.dbng;

public class ReplayContext : DbContext
{
    public virtual DbSet<Uploader> Uploaders { get; set; } = null!;
    public virtual DbSet<BattleNetInfo> BattleNetInfos { get; set; } = null!;
    public virtual DbSet<Player> Players { get; set; } = null!;
    public virtual DbSet<NoUploadResult> NoUploadResults { get; set; } = null!;
    public virtual DbSet<PlayerRating> PlayerRatings { get; set; } = null!;
    public virtual DbSet<PlayerRatingChange> PlayerRatingChanges { get; set; } = null!;
    public virtual DbSet<Replay> Replays { get; set; } = null!;
    public virtual DbSet<ReplayPlayer> ReplayPlayers { get; set; } = null!;
    public virtual DbSet<ReplayRating> ReplayRatings { get; set; } = null!;
    public virtual DbSet<RepPlayerRating> RepPlayerRatings { get; set; } = null!;
    public virtual DbSet<PlayerUpgrade> PlayerUpgrades { get; set; } = null!;
    public virtual DbSet<Spawn> Spawns { get; set; } = null!;
    public virtual DbSet<SpawnUnit> SpawnUnits { get; set; } = null!;
    public virtual DbSet<Unit> Units { get; set; } = null!;
    public virtual DbSet<Upgrade> Upgrades { get; set; } = null!;
    public virtual DbSet<ReplayEvent> ReplayEvents { get; set; } = null!;
    public virtual DbSet<Event> Events { get; set; } = null!;
    public virtual DbSet<ReplayViewCount> ReplayViewCounts { get; set; } = null!;
    public virtual DbSet<ReplayDownloadCount> ReplayDownloadCounts { get; set; } = null!;
    public virtual DbSet<SkipReplay> SkipReplays { get; set; } = null!;
    public virtual DbSet<CommanderMmr> CommanderMmrs { get; set; } = null!;
    public virtual DbSet<GroupByHelper> GroupByHelpers { get; set; } = null!;
    public virtual DbSet<FunStatsMemory> FunStatMemories { get; set; } = null!;
    public virtual DbSet<ArcadeReplay> ArcadeReplays { get; set; } = null!;
    public virtual DbSet<ArcadeReplayPlayer> ArcadeReplayPlayers { get; set; } = null!;
    public virtual DbSet<ArcadePlayer> ArcadePlayers { get; set; } = null!;
    public virtual DbSet<ArcadeReplayRating> ArcadeReplayRatings { get; set; } = null!;
    public virtual DbSet<ArcadePlayerRating> ArcadePlayerRatings { get; set; } = null!;
    public virtual DbSet<ArcadeReplayPlayerRating> ArcadeReplayPlayerRatings { get; set; } = null!;
    public virtual DbSet<ArcadePlayerRatingChange> ArcadePlayerRatingChanges { get; set; } = null!;

    public ReplayContext(DbContextOptions<ReplayContext> options)
    : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Replay>(entity =>
        {
            entity.HasIndex(e => e.FileName);
            entity.HasIndex(e => e.Maxkillsum);
            entity.HasIndex(e => new { e.GameTime, e.GameMode });
            entity.HasIndex(e => new { e.GameTime, e.GameMode, e.DefaultFilter });
            entity.HasIndex(e => new { e.GameTime, e.GameMode, e.WinnerTeam });
            entity.HasIndex(e => new { e.GameTime, e.GameMode, e.Maxleaver });
            entity.HasIndex(e => e.Imported);

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
            entity.HasIndex(e => e.Kills);
            entity.HasIndex(e => new { e.IsUploader, e.Team });
            entity.HasIndex(e => e.Name);

            entity.Property(p => p.LastSpawnHash)
                .HasMaxLength(64)
                .IsFixedLength();
            entity.HasIndex(e => e.LastSpawnHash)
                .IsUnique();
        });

        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasIndex(e => e.Name).IsUnique();
        });

        modelBuilder.Entity<Player>(entity =>
        {
            entity.HasIndex(e => new { e.RegionId, e.RealmId, e.ToonId }).IsUnique();
        });

        modelBuilder.Entity<Uploader>(entity =>
        {
            entity.HasIndex(e => e.AppGuid).IsUnique();
        });

        modelBuilder.Entity<Uploader>()
            .HasMany(p => p.Replays)
            .WithMany(p => p.Uploaders)
            .UsingEntity(j => j.ToTable("UploaderReplays"));

        modelBuilder.Entity<Unit>(entity =>
        {
            entity.HasIndex(e => e.Name).IsUnique();
        });

        modelBuilder.Entity<Upgrade>(entity =>
        {
            entity.HasIndex(e => e.Name).IsUnique();
        });

        modelBuilder.Entity<CommanderMmr>(entity =>
        {
            entity.HasIndex(e => new { e.Race, e.OppRace });
        });

        modelBuilder.Entity<GroupByHelper>(entity =>
        {
            entity.HasNoKey();
            entity.ToView("GroupByHelper");
            entity.Property(p => p.Group).HasColumnName("Name");
        });

        modelBuilder.Entity<PlayerRating>(entity =>
        {
            entity.HasIndex(e => e.RatingType);
        });

        modelBuilder.Entity<ArcadeReplay>(entity =>
        {
            entity.HasIndex(i => new { i.GameMode, i.CreatedAt });
            entity.HasIndex(i => new { i.RegionId, i.GameMode, i.CreatedAt });
            entity.HasIndex(i => i.Id);
        });

        modelBuilder.Entity<ArcadePlayer>(entity =>
        {
            entity.HasIndex(i => i.Name);
            entity.HasIndex(i => new { i.RegionId, i.RealmId, i.ProfileId }).IsUnique();
        });
    }
}
