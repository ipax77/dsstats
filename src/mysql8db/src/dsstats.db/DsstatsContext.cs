using System.ComponentModel.DataAnnotations;
using dsstats.db8;
using System.Reflection;
using dsstats.shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Storage;

namespace dsstats.db;

public sealed class DsstatsContext : DbContext
{
    public DbSet<Replay> Replays { get; set; }
    public DbSet<ReplayPlayer> ReplayPlayers { get; set; }
    public DbSet<Player> Players { get; set; }
    public DbSet<PlayerRating> PlayerRatings { get; set; }
    public DbSet<ReplayRating> ReplayRatings { get; set; }
    public DbSet<ReplayPlayerRating> ReplayPlayerRatings { get; set; }
    public DbSet<Spawn> Spawns { get; set; }
    public DbSet<SpawnUnit> SpawnUnits { get; set; }
    public DbSet<Unit> Units { get; set; }
    public DbSet<PlayerUpgrade> PlayerUpgrades { get; set; }
    public DbSet<Upgrade> Upgrades { get; set; }
    public DbSet<ArcadeReplay> ArcadeReplays { get; set; }
    public DbSet<ArcadeReplayPlayer> ArcadeReplayPlayers { get; set; }
    public DbSet<ReplayArcadeMatch> ReplayArcadeMatches { get; set; }
    public int Week(DateTime date) => throw new InvalidOperationException($"{nameof(Week)} cannot be called client side.");
    public int Strftime(string arg, DateTime date) => throw new InvalidOperationException($"{nameof(Strftime)} cannot be called client side.");

    public DsstatsContext(DbContextOptions<DsstatsContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Replay>(entity =>
        {
            entity.HasIndex(e => new { e.GameTime });
            entity.HasIndex(e => new { e.GameTime, e.GameMode });
            entity.HasIndex(e => e.Imported);
            entity.Property(p => p.ReplayHash)
                .HasMaxLength(64)
                .IsFixedLength();
            entity.HasIndex(i => i.ReplayHash).IsUnique();
        });

        modelBuilder.Entity<ReplayPlayer>(entity =>
        {
            entity.HasIndex(e => e.Race);
            entity.HasIndex(e => e.Name);
            entity.Property(p => p.LastSpawnHash)
                .HasMaxLength(64)
                .IsFixedLength();
            entity.HasIndex(e => e.LastSpawnHash)
                .IsUnique();
            entity.HasOne(o => o.Opponent)
                .WithMany()
                .HasForeignKey("OpponentId")
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Player>(entity =>
        {
            entity.HasIndex(e => new { e.RegionId, e.RealmId, e.ToonId }).IsUnique();
        });

        modelBuilder.Entity<PlayerRating>(entity =>
        {
            entity.HasIndex(e => new { e.PlayerId, e.RatingType }).IsUnique();
        });

        modelBuilder.Entity<ReplayRating>(entity =>
        {
            entity.HasIndex(i => new { i.RatingType, i.ReplayId }).IsUnique();
        });

        modelBuilder.Entity<ReplayPlayerRating>(entity =>
        {
            entity.HasIndex(i => new { i.RatingType, i.ReplayPlayerId }).IsUnique();
        });

        modelBuilder.Entity<Unit>(entity =>
        {
            entity.HasIndex(e => e.Name).IsUnique();
        });

        modelBuilder.Entity<Upgrade>(entity =>
        {
            entity.HasIndex(e => e.Name).IsUnique();
        });

        modelBuilder.Entity<ArcadeReplay>(entity =>
        {
            entity.HasIndex(i => new { i.CreatedAt });
            entity.HasIndex(i => new { i.Imported });
            entity.HasIndex(i => new { i.RegionId, i.BnetBucketId, i.BnetRecordId }).IsUnique();
        });

        MethodInfo weekMethodInfo = typeof(ReplayContext)
    .GetRuntimeMethod(nameof(ReplayContext.Week), new[] { typeof(DateTime) }) ?? throw new ArgumentNullException();

        modelBuilder.HasDbFunction(weekMethodInfo)
           .HasTranslation(args =>
                    new SqlFunctionExpression("WEEK",
                        new[]
                        {
                            args.ToArray()[0],
                            new SqlConstantExpression(Expression.Constant(3, typeof(int)), new IntTypeMapping("int")),
                        },
                        true,
                        new[] { false, false },
                        typeof(int),
                        null
                    )
                );

        MethodInfo strftimeMethodInfo = typeof(ReplayContext)
            .GetRuntimeMethod(nameof(ReplayContext.Strftime), new[] { typeof(string), typeof(DateTime) }) ?? throw new ArgumentNullException();

        modelBuilder.HasDbFunction(strftimeMethodInfo)
           .HasTranslation(args =>
                    new SqlFunctionExpression("strftime",
                        new[]
                        {
                            new SqlFragmentExpression((args.ToArray()[0] as SqlConstantExpression)?.Value?.ToString() ?? string.Empty),
                            args.ToArray()[1]
                        },
                        true,
                        new[] { false, false },
                        typeof(int),
                        null
                    )
                );
    }
}

public sealed class ReplayArcadeMatch
{
    public int ReplayArcadeMatchId { get; set; }
    [Precision(0)]
    public DateTime MatchTime { get; set; }
    public int ReplayId { get; set; }
    public Replay? Replay { get; set; }
    public int ArcadeReplayId { get; set; }
    public ArcadeReplay? ArcadeReplay { get; set; }
}

public class MaterializedArcadeReplay
{
    [Key]
    public int MaterializedArcadeReplayId { get; set; }
    public int ArcadeReplayId { get; set; }
    public GameMode GameMode { get; set; }
    [Precision(0)]
    public DateTime CreatedAt { get; set; }
    public int Duration { get; set; }
    public int WinnerTeam { get; set; }
}

public sealed class ArcadeReplay
{
    public int ArcadeReplayId { get; set; }
    public int RegionId { get; set; }
    public long BnetBucketId { get; set; }
    public long BnetRecordId { get; set; }
    public GameMode GameMode { get; set; }
    [Precision(0)]
    public DateTime CreatedAt { get; set; }
    public int Duration { get; set; }
    public int PlayerCount { get; set; }
    public int WinnerTeam { get; set; }
    [Precision(0)]
    public DateTime Imported { get; set; }
    public ICollection<ArcadeReplayPlayer> ArcadeReplayPlayers { get; set; } = [];
}

public sealed class ArcadeReplayPlayer
{
    public int ArcadeReplayPlayerId { get; set; }
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;
    public int SlotNumber { get; set; }
    public int Team { get; set; }
    public int Discriminator { get; set; }
    public PlayerResult PlayerResult { get; set; }
    public int ArcadeReplayId { get; set; }
    public ArcadeReplay? ArcadeReplay { get; set; }
    public int PlayerId { get; set; }
    public Player? Player { get; set; }
}



