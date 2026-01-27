using dsstats.db.UnitModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;
using System.Reflection;

namespace dsstats.db;

public class StagingDsstatsContext : DsstatsContext
{
    public StagingDsstatsContext(DbContextOptions<DsstatsContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PlayerRating>().ToTable("PlayerRatings_tmp");
        modelBuilder.Entity<ReplayRating>().ToTable("ReplayRatings_tmp");
        modelBuilder.Entity<ReplayPlayerRating>().ToTable("ReplayPlayerRatings_tmp");

        base.OnModelCreating(modelBuilder);
    }
};

public class DsstatsContext : DbContext
{
    public DbSet<Replay> Replays { get; set; }
    public DbSet<ReplayPlayer> ReplayPlayers { get; set; }
    public DbSet<Player> Players { get; set; }
    public DbSet<Spawn> Spawns { get; set; }
    public DbSet<SpawnUnit> SpawnUnits { get; set; }
    public DbSet<Unit> Units { get; set; }
    public DbSet<PlayerUpgrade> PlayerUpgrades { get; set; }
    public DbSet<Upgrade> Upgrades { get; set; }
    public DbSet<PlayerRating> PlayerRatings { get; set; }
    public DbSet<ReplayRating> ReplayRatings { get; set; }
    public DbSet<ReplayPlayerRating> ReplayPlayerRatings { get; set; }
    public DbSet<ArcadeReplay> ArcadeReplays { get; set; }
    public DbSet<ArcadeReplayPlayer> ArcadeReplayPlayers { get; set; }
    public DbSet<ReplayArcadeMatch> ReplayArcadeMatches { get; set; }
    public DbSet<CombinedReplay> CombinedReplays { get; set; }
    public DbSet<ArcadeReplayRating> ArcadeReplayRatings { get; set; }
    public DbSet<UploadJob> UploadJobs { get; set; }
    public DbSet<ReplayUploadJob> ReplayUploadJobs { get; set; }
    public DbSet<DsUnit> DsUnits { get; set; }
    public DbSet<DsWeapon> DsWeapons { get; set; }
    public DbSet<BonusDamage> BonusDamages { get; set; }
    public DbSet<DsAbility> DsAbilities { get; set; }
    public DbSet<DsUpgrade> DsUpgrades { get; set; }

    public DbSet<MauiConfig> MauiConfig { get; set; }
    public DbSet<Sc2Profile> Sc2Profiles { get; set; }

    public int Week(DateTime date) => throw new InvalidOperationException($"{nameof(Week)} cannot be called client side.");

    public DsstatsContext(DbContextOptions<DsstatsContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Replay>(entity =>
        {
            entity.HasIndex(i => i.ReplayHash).IsUnique();
            entity.HasIndex(i => i.CompatHash);
            entity.HasIndex(i => i.Gametime);
            entity.HasIndex(i => new { i.Gametime, i.ReplayId });
            entity.HasIndex(i => new { i.Gametime, i.Duration, i.WinnerTeam, i.PlayerCount, i.GameMode, i.TE });
        });

        modelBuilder.Entity<Player>(entity =>
        {
            entity.OwnsOne(p => p.ToonId, toon =>
            {
                toon.Property(t => t.Region).IsRequired();
                toon.Property(t => t.Realm).IsRequired();
                toon.Property(t => t.Id).IsRequired();

                toon.HasIndex(t => new { t.Region, t.Realm, t.Id }).IsUnique();
            });

            entity.HasIndex(i => i.Name);
        });

        modelBuilder.Entity<Sc2Profile>(entity =>
        {
            entity.OwnsOne(p => p.ToonId, toon =>
            {
                toon.Property(t => t.Region).IsRequired();
                toon.Property(t => t.Realm).IsRequired();
                toon.Property(t => t.Id).IsRequired();

                toon.HasIndex(t => new { t.Region, t.Realm, t.Id });
            });

            entity.HasIndex(i => i.Name);
        });

        modelBuilder.Entity<ReplayPlayer>(entity =>
        {
        });

        modelBuilder.Entity<Unit>(entity =>
        {
            entity.HasIndex(u => u.Name).IsUnique();
        });

        modelBuilder.Entity<Upgrade>(entity =>
        {
            entity.HasIndex(u => u.Name).IsUnique();
        });

        modelBuilder.Entity<ArcadeReplay>(entity =>
        {
            entity.HasIndex(i => new { i.CreatedAt, i.ArcadeReplayId })
                .IsDescending(true, false);

            entity.HasIndex(i => new { i.CreatedAt, i.ArcadeReplayId })
                .IsDescending(false, true);

            entity.HasIndex(i => new { i.RegionId, i.BnetBucketId, i.BnetRecordId }).IsUnique();
        });

        modelBuilder.Entity<CombinedReplay>(entity =>
        {
            entity.HasIndex(i => i.Gametime);
            entity.HasIndex(i => i.ReplayId).IsUnique();
            entity.HasIndex(i => i.ArcadeReplayId).IsUnique();
            entity.HasIndex(i => i.Imported);
        });

        modelBuilder.Entity<PlayerRating>(entity =>
        {
            entity.HasIndex(x => new { x.PlayerId, x.RatingType })
                .IsUnique();
            entity.HasIndex(i => i.RatingType);
            entity.HasIndex(i => i.Rating);
            entity.HasIndex(i => i.LastGame);
        });

        modelBuilder.Entity<UploadJob>(entity =>
        {
            entity.HasIndex(x => x.FinishedAt);
            entity.HasIndex(x => x.CreatedAt);
        });

        modelBuilder.Entity<ReplayUploadJob>(entity =>
        {
            entity.HasIndex(x => x.FinishedAt);
            entity.HasIndex(x => x.CreatedAt);
        });

        modelBuilder.Entity<ReplayRating>(entity =>
        {
            entity.HasIndex(x => new { x.ReplayId, x.RatingType })
            .IsUnique();

            entity.HasIndex(i => i.IsPreRating);
        });

        modelBuilder.Entity<ReplayPlayerRating>()
            .HasIndex(x => new { x.ReplayRatingId, x.ReplayPlayerId, x.RatingType })
            .IsUnique();

        modelBuilder.Entity<ReplayIdResult>().HasNoKey();

        MethodInfo weekMethodInfo = typeof(DsstatsContext)
            .GetRuntimeMethod(nameof(DsstatsContext.Week), new[] { typeof(DateTime) }) ?? throw new ArgumentNullException();

        modelBuilder.HasDbFunction(weekMethodInfo)
           .HasTranslation(args =>
                    new SqlFunctionExpression("WEEK",
                        new[]
                        {
                    args.ToArray()[0],
                    new SqlConstantExpression(3, typeof(int), new IntTypeMapping("int")),
                        },
                        true,
                        new[] { false, false },
                        typeof(int),
                        null
                    )
                );

    }
}
