﻿using dsstats.db8.Challenge;
using dsstats.shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace dsstats.db8;

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
    public virtual DbSet<MaterializedArcadeReplay> MaterializedArcadeReplays { get; set; } = null!;
    public virtual DbSet<ArcadeReplayDsPlayer> ArcadeReplayDsPlayers { get; set; } = null!;
    public virtual DbSet<ArcadeReplayRating> ArcadeReplayRatings { get; set; } = null!;
    public virtual DbSet<ArcadePlayerRating> ArcadePlayerRatings { get; set; } = null!;
    public virtual DbSet<ArcadeReplayDsPlayerRating> ArcadeReplayDsPlayerRatings { get; set; } = null!;
    public virtual DbSet<ArcadePlayerRatingChange> ArcadePlayerRatingChanges { get; set; } = null!;
    public virtual DbSet<DsUpdate> DsUpdates { get; set; } = null!;
    public virtual DbSet<DsUnit> DsUnits { get; set; } = null!;
    public virtual DbSet<DsWeapon> DsWeapons { get; set; } = null!;
    public virtual DbSet<BonusDamage> BonusDamages { get; set; } = null!;
    public virtual DbSet<DsAbility> DsAbilities { get; set; } = null!;
    public virtual DbSet<DsUpgrade> DsUpgrades { get; set; } = null!;
    public virtual DbSet<ReplayArcadeMatch> ReplayArcadeMatches { get; set; } = null!;

    public virtual DbSet<ComboPlayerRating> ComboPlayerRatings { get; set; } = null!;
    public virtual DbSet<ComboReplayRating> ComboReplayRatings { get; set; } = null!;
    public virtual DbSet<ComboReplayPlayerRating> ComboReplayPlayerRatings { get; set; } = null!;
    public virtual DbSet<Faq> Faqs { get; set; } = null!;
    public virtual DbSet<FaqVote> FaqVotes { get; set; } = null!;
    public virtual DbSet<IhSession> IhSessions { get; set; } = null!;
    public virtual DbSet<IhSessionPlayer> IhSessionPlayers { get; set; } = null!;
    public virtual DbSet<DsPickBan> DsPickBans { get; set; } = null!;
    public virtual DbSet<SpChallenge> SpChallenges { get; set; } = null!;
    public virtual DbSet<SpChallengeSubmission> SpChallengeSubmissions { get; set; } = null!;
    public int Week(DateTime date) => throw new InvalidOperationException($"{nameof(Week)} cannot be called client side.");
    public int Strftime(string arg, DateTime date) => throw new InvalidOperationException($"{nameof(Strftime)} cannot be called client side.");

    public virtual DbSet<StreakInfo> StreakInfos { get; set; } = null!;

    public ReplayContext(DbContextOptions<ReplayContext> options)
    : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StreakInfo>(entity =>
        {
            entity.HasNoKey();
        });

        modelBuilder.Entity<Replay>(entity =>
        {
            entity.HasIndex(e => e.FileName);
            entity.HasIndex(e => e.Maxkillsum);
            entity.HasIndex(e => new { e.GameTime });
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

        modelBuilder.Entity<ComboPlayerRating>(entity =>
        {
            entity.HasIndex(i => i.RatingType);
        });

        modelBuilder.Entity<ComboReplayRating>(entity =>
        {
            entity.HasIndex(i => i.RatingType);
        });

        modelBuilder.Entity<PlayerRating>(entity =>
        {
            entity.HasIndex(e => e.RatingType);
        });

        modelBuilder.Entity<ArcadeReplay>(entity =>
        {
            entity.HasIndex(i => new { i.GameMode, i.CreatedAt });
            entity.HasIndex(i => new { i.RegionId, i.GameMode, i.CreatedAt });
            entity.HasIndex(i => new { i.RegionId, i.BnetBucketId, i.BnetRecordId }).IsUnique();
            entity.HasIndex(i => i.ReplayHash);
        });

        modelBuilder.Entity<ArcadePlayerRating>(entity =>
        {
            entity.HasIndex(i => i.RatingType);
        });

        modelBuilder.Entity<ReplayRating>(entity =>
        {
            entity.HasIndex(i => i.RatingType);
        });

        modelBuilder.Entity<DsUpdate>(entity =>
        {
            entity.HasIndex(i => i.Time);
        });

        modelBuilder.Entity<DsUnit>(entity =>
        {
            entity.HasIndex(i => i.Name);
            entity.HasIndex(i => i.Commander);
            entity.HasIndex(i => new { i.Name, i.Commander });
        });

        modelBuilder.Entity<BonusDamage>(entity =>
        {
            entity.HasIndex(i => i.UnitType);
        });

        modelBuilder.Entity<DsAbility>(entity =>
        {
            entity.HasIndex(i => i.Name);
        });

        modelBuilder.Entity<DsUpgrade>(entity =>
        {
            entity.HasIndex(i => i.Upgrade);
        });

        modelBuilder.Entity<Faq>(entity =>
        {
            entity.HasIndex(i => i.Question);
        });

        modelBuilder.Entity<IhSession>(entity =>
        {
            entity.HasIndex(i => i.GroupId).IsUnique();
            entity.Property(p => p.GroupState).HasConversion(
                c => JsonSerializer.Serialize(c, (JsonSerializerOptions?)null),
                c => JsonSerializer.Deserialize<GroupState>(c, (JsonSerializerOptions?)null));
            entity.Property(p => p.GroupStateV2).HasConversion(
                c => JsonSerializer.Serialize(c, (JsonSerializerOptions?)null),
                c => JsonSerializer.Deserialize<GroupStateV2>(c, (JsonSerializerOptions?)null));
        });

        modelBuilder.Entity<ReplayArcadeMatch>(entity =>
        {
            entity.HasIndex(i => i.ReplayId).IsUnique();
            entity.HasIndex(i => i.ArcadeReplayId).IsUnique();
            entity.HasIndex(i => i.MatchTime);
        });

        modelBuilder.Entity<MaterializedArcadeReplay>(entity =>
        {
            entity.HasIndex(i => i.CreatedAt);
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

public record StreakInfo
{
    public int PlayerResult { get; set; }
    public double LongestStreak { get; set; }
}