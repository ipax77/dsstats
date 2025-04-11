﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using dsstats.db;

#nullable disable

namespace MySqlMigrations.Migrations
{
    [DbContext(typeof(DsstatsContext))]
    [Migration("20250411034931_GameCounts")]
    partial class GameCounts
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.14")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            MySqlModelBuilderExtensions.AutoIncrementColumns(modelBuilder);

            modelBuilder.Entity("dsstats.db.ArcadeReplay", b =>
                {
                    b.Property<int>("ArcadeReplayId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("ArcadeReplayId"));

                    b.Property<long>("BnetBucketId")
                        .HasColumnType("bigint");

                    b.Property<long>("BnetRecordId")
                        .HasColumnType("bigint");

                    b.Property<DateTime>("CreatedAt")
                        .HasPrecision(0)
                        .HasColumnType("datetime(0)");

                    b.Property<int>("Duration")
                        .HasColumnType("int");

                    b.Property<int>("GameMode")
                        .HasColumnType("int");

                    b.Property<DateTime>("Imported")
                        .HasPrecision(0)
                        .HasColumnType("datetime(0)");

                    b.Property<int>("PlayerCount")
                        .HasColumnType("int");

                    b.Property<int>("RegionId")
                        .HasColumnType("int");

                    b.Property<int>("WinnerTeam")
                        .HasColumnType("int");

                    b.HasKey("ArcadeReplayId");

                    b.HasIndex("CreatedAt");

                    b.HasIndex("RegionId", "BnetBucketId", "BnetRecordId")
                        .IsUnique();

                    b.ToTable("ArcadeReplays");
                });

            modelBuilder.Entity("dsstats.db.ArcadeReplayPlayer", b =>
                {
                    b.Property<int>("ArcadeReplayPlayerId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("ArcadeReplayPlayerId"));

                    b.Property<int>("ArcadeReplayId")
                        .HasColumnType("int");

                    b.Property<int>("Discriminator")
                        .HasColumnType("int");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("varchar(50)");

                    b.Property<int>("PlayerId")
                        .HasColumnType("int");

                    b.Property<int>("PlayerResult")
                        .HasColumnType("int");

                    b.Property<int>("SlotNumber")
                        .HasColumnType("int");

                    b.Property<int>("Team")
                        .HasColumnType("int");

                    b.HasKey("ArcadeReplayPlayerId");

                    b.HasIndex("ArcadeReplayId");

                    b.HasIndex("PlayerId");

                    b.ToTable("ArcadeReplayPlayers");
                });

            modelBuilder.Entity("dsstats.db.Player", b =>
                {
                    b.Property<int>("PlayerId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("PlayerId"));

                    b.Property<int>("GlobalRating")
                        .HasColumnType("int");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("varchar(50)");

                    b.Property<int>("RealmId")
                        .HasColumnType("int");

                    b.Property<int>("RegionId")
                        .HasColumnType("int");

                    b.Property<int>("ToonId")
                        .HasColumnType("int");

                    b.HasKey("PlayerId");

                    b.HasIndex("RegionId", "RealmId", "ToonId")
                        .IsUnique();

                    b.ToTable("Players");
                });

            modelBuilder.Entity("dsstats.db.PlayerRating", b =>
                {
                    b.Property<int>("PlayerRatingId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("PlayerRatingId"));

                    b.Property<int>("ArcadeGames")
                        .HasColumnType("int");

                    b.Property<double>("Confidence")
                        .HasColumnType("double");

                    b.Property<double>("Consistency")
                        .HasColumnType("double");

                    b.Property<int>("DsstatsGames")
                        .HasColumnType("int");

                    b.Property<int>("Games")
                        .HasColumnType("int");

                    b.Property<int>("Mvp")
                        .HasColumnType("int");

                    b.Property<int>("PlayerId")
                        .HasColumnType("int");

                    b.Property<int>("Pos")
                        .HasColumnType("int");

                    b.Property<double>("Rating")
                        .HasColumnType("double");

                    b.Property<int>("RatingType")
                        .HasColumnType("int");

                    b.Property<int>("Wins")
                        .HasColumnType("int");

                    b.HasKey("PlayerRatingId");

                    b.HasIndex("PlayerId", "RatingType")
                        .IsUnique();

                    b.ToTable("PlayerRatings");
                });

            modelBuilder.Entity("dsstats.db.PlayerUpgrade", b =>
                {
                    b.Property<int>("PlayerUpgradeId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("PlayerUpgradeId"));

                    b.Property<int>("Gameloop")
                        .HasColumnType("int");

                    b.Property<int>("ReplayPlayerId")
                        .HasColumnType("int");

                    b.Property<int>("UpgradeId")
                        .HasColumnType("int");

                    b.HasKey("PlayerUpgradeId");

                    b.HasIndex("ReplayPlayerId");

                    b.HasIndex("UpgradeId");

                    b.ToTable("PlayerUpgrades");
                });

            modelBuilder.Entity("dsstats.db.Replay", b =>
                {
                    b.Property<int>("ReplayId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("ReplayId"));

                    b.Property<int>("Bunker")
                        .HasColumnType("int");

                    b.Property<int>("Cannon")
                        .HasColumnType("int");

                    b.Property<string>("CommandersTeam1")
                        .IsRequired()
                        .HasMaxLength(15)
                        .HasColumnType("varchar(15)");

                    b.Property<string>("CommandersTeam2")
                        .IsRequired()
                        .HasMaxLength(15)
                        .HasColumnType("varchar(15)");

                    b.Property<int>("Duration")
                        .HasColumnType("int");

                    b.Property<int>("GameMode")
                        .HasColumnType("int");

                    b.Property<DateTime>("GameTime")
                        .HasPrecision(0)
                        .HasColumnType("datetime(0)");

                    b.Property<DateTime?>("Imported")
                        .HasPrecision(0)
                        .HasColumnType("datetime(0)");

                    b.Property<bool>("IsTE")
                        .HasColumnType("tinyint(1)");

                    b.Property<int>("Maxkillsum")
                        .HasColumnType("int");

                    b.Property<int>("Maxleaver")
                        .HasColumnType("int");

                    b.Property<byte[]>("MiddleBinary")
                        .IsRequired()
                        .HasColumnType("longblob");

                    b.Property<int>("Minarmy")
                        .HasColumnType("int");

                    b.Property<int>("Minincome")
                        .HasColumnType("int");

                    b.Property<int>("Minkillsum")
                        .HasColumnType("int");

                    b.Property<int>("PlayerCount")
                        .HasColumnType("int");

                    b.Property<int>("Region")
                        .HasColumnType("int");

                    b.Property<string>("ReplayHash")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("char(64)")
                        .IsFixedLength();

                    b.Property<int>("Views")
                        .HasColumnType("int");

                    b.Property<int>("WinnerTeam")
                        .HasColumnType("int");

                    b.HasKey("ReplayId");

                    b.HasIndex("GameTime");

                    b.HasIndex("Imported");

                    b.HasIndex("ReplayHash")
                        .IsUnique();

                    b.HasIndex("GameTime", "GameMode");

                    b.ToTable("Replays");
                });

            modelBuilder.Entity("dsstats.db.ReplayArcadeMatch", b =>
                {
                    b.Property<int>("ReplayArcadeMatchId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("ReplayArcadeMatchId"));

                    b.Property<int>("ArcadeReplayId")
                        .HasColumnType("int");

                    b.Property<DateTime>("MatchTime")
                        .HasPrecision(0)
                        .HasColumnType("datetime(0)");

                    b.Property<int>("ReplayId")
                        .HasColumnType("int");

                    b.HasKey("ReplayArcadeMatchId");

                    b.HasIndex("ArcadeReplayId");

                    b.HasIndex("ReplayId");

                    b.ToTable("ReplayArcadeMatches");
                });

            modelBuilder.Entity("dsstats.db.ReplayPlayer", b =>
                {
                    b.Property<int>("ReplayPlayerId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("ReplayPlayerId"));

                    b.Property<int>("APM")
                        .HasColumnType("int");

                    b.Property<int>("Army")
                        .HasColumnType("int");

                    b.Property<string>("Clan")
                        .HasMaxLength(50)
                        .HasColumnType("varchar(50)");

                    b.Property<int>("Duration")
                        .HasColumnType("int");

                    b.Property<int>("GamePos")
                        .HasColumnType("int");

                    b.Property<int>("Income")
                        .HasColumnType("int");

                    b.Property<bool>("IsUploader")
                        .HasColumnType("tinyint(1)");

                    b.Property<int>("Kills")
                        .HasColumnType("int");

                    b.Property<string>("LastSpawnHash")
                        .HasMaxLength(64)
                        .HasColumnType("char(64)")
                        .IsFixedLength();

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("varchar(50)");

                    b.Property<int?>("OpponentId")
                        .HasColumnType("int");

                    b.Property<int>("PlayerId")
                        .HasColumnType("int");

                    b.Property<int>("PlayerResult")
                        .HasColumnType("int");

                    b.Property<int>("Race")
                        .HasColumnType("int");

                    b.Property<string>("Refineries")
                        .IsRequired()
                        .HasMaxLength(300)
                        .HasColumnType("varchar(300)");

                    b.Property<int>("ReplayId")
                        .HasColumnType("int");

                    b.Property<int>("Team")
                        .HasColumnType("int");

                    b.Property<string>("TierUpgrades")
                        .IsRequired()
                        .HasMaxLength(300)
                        .HasColumnType("varchar(300)");

                    b.Property<int>("UpgradesSpent")
                        .HasColumnType("int");

                    b.HasKey("ReplayPlayerId");

                    b.HasIndex("LastSpawnHash")
                        .IsUnique();

                    b.HasIndex("Name");

                    b.HasIndex("OpponentId");

                    b.HasIndex("PlayerId");

                    b.HasIndex("Race");

                    b.HasIndex("ReplayId");

                    b.ToTable("ReplayPlayers");
                });

            modelBuilder.Entity("dsstats.db.ReplayPlayerRating", b =>
                {
                    b.Property<int>("ReplayPlayerRatingId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("ReplayPlayerRatingId"));

                    b.Property<decimal>("Change")
                        .HasPrecision(5, 2)
                        .HasColumnType("decimal(5,2)");

                    b.Property<decimal>("Confidence")
                        .HasPrecision(5, 2)
                        .HasColumnType("decimal(5,2)");

                    b.Property<decimal>("Consistency")
                        .HasPrecision(5, 2)
                        .HasColumnType("decimal(5,2)");

                    b.Property<int>("GamePos")
                        .HasColumnType("int");

                    b.Property<int>("Games")
                        .HasColumnType("int");

                    b.Property<int>("Rating")
                        .HasColumnType("int");

                    b.Property<int>("RatingType")
                        .HasColumnType("int");

                    b.Property<int>("ReplayPlayerId")
                        .HasColumnType("int");

                    b.HasKey("ReplayPlayerRatingId");

                    b.HasIndex("ReplayPlayerId");

                    b.HasIndex("RatingType", "ReplayPlayerId")
                        .IsUnique();

                    b.ToTable("ReplayPlayerRatings");
                });

            modelBuilder.Entity("dsstats.db.ReplayRating", b =>
                {
                    b.Property<int>("ReplayRatingId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("ReplayRatingId"));

                    b.Property<int>("AvgRating")
                        .HasColumnType("int");

                    b.Property<decimal>("ExpectationToWin")
                        .HasPrecision(5, 2)
                        .HasColumnType("decimal(5,2)");

                    b.Property<bool>("IsPreRating")
                        .HasColumnType("tinyint(1)");

                    b.Property<int>("LeaverType")
                        .HasColumnType("int");

                    b.Property<int>("RatingType")
                        .HasColumnType("int");

                    b.Property<int>("ReplayId")
                        .HasColumnType("int");

                    b.HasKey("ReplayRatingId");

                    b.HasIndex("ReplayId");

                    b.HasIndex("RatingType", "ReplayId")
                        .IsUnique();

                    b.ToTable("ReplayRatings");
                });

            modelBuilder.Entity("dsstats.db.Spawn", b =>
                {
                    b.Property<int>("SpawnId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("SpawnId"));

                    b.Property<int>("ArmyValue")
                        .HasColumnType("int");

                    b.Property<int>("Breakpoint")
                        .HasColumnType("int");

                    b.Property<int>("Gameloop")
                        .HasColumnType("int");

                    b.Property<int>("GasCount")
                        .HasColumnType("int");

                    b.Property<int>("Income")
                        .HasColumnType("int");

                    b.Property<int>("KilledValue")
                        .HasColumnType("int");

                    b.Property<int>("ReplayPlayerId")
                        .HasColumnType("int");

                    b.Property<int>("UpgradeSpent")
                        .HasColumnType("int");

                    b.HasKey("SpawnId");

                    b.HasIndex("ReplayPlayerId");

                    b.ToTable("Spawns");
                });

            modelBuilder.Entity("dsstats.db.SpawnUnit", b =>
                {
                    b.Property<int>("SpawnUnitId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("SpawnUnitId"));

                    b.Property<int>("Count")
                        .HasColumnType("int");

                    b.Property<byte[]>("PositionsBinary")
                        .IsRequired()
                        .HasColumnType("longblob");

                    b.Property<int>("SpawnId")
                        .HasColumnType("int");

                    b.Property<int>("UnitId")
                        .HasColumnType("int");

                    b.HasKey("SpawnUnitId");

                    b.HasIndex("SpawnId");

                    b.HasIndex("UnitId");

                    b.ToTable("SpawnUnits");
                });

            modelBuilder.Entity("dsstats.db.Unit", b =>
                {
                    b.Property<int>("UnitId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("UnitId"));

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("varchar(50)");

                    b.HasKey("UnitId");

                    b.HasIndex("Name")
                        .IsUnique();

                    b.ToTable("Units");
                });

            modelBuilder.Entity("dsstats.db.Upgrade", b =>
                {
                    b.Property<int>("UpgradeId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("UpgradeId"));

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("varchar(50)");

                    b.HasKey("UpgradeId");

                    b.HasIndex("Name")
                        .IsUnique();

                    b.ToTable("Upgrades");
                });

            modelBuilder.Entity("dsstats.db.ArcadeReplayPlayer", b =>
                {
                    b.HasOne("dsstats.db.ArcadeReplay", "ArcadeReplay")
                        .WithMany("ArcadeReplayPlayers")
                        .HasForeignKey("ArcadeReplayId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("dsstats.db.Player", "Player")
                        .WithMany()
                        .HasForeignKey("PlayerId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("ArcadeReplay");

                    b.Navigation("Player");
                });

            modelBuilder.Entity("dsstats.db.PlayerRating", b =>
                {
                    b.HasOne("dsstats.db.Player", "Player")
                        .WithMany("PlayerRatings")
                        .HasForeignKey("PlayerId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Player");
                });

            modelBuilder.Entity("dsstats.db.PlayerUpgrade", b =>
                {
                    b.HasOne("dsstats.db.ReplayPlayer", "ReplayPlayer")
                        .WithMany("PlayerUpgrades")
                        .HasForeignKey("ReplayPlayerId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("dsstats.db.Upgrade", "Upgrade")
                        .WithMany()
                        .HasForeignKey("UpgradeId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("ReplayPlayer");

                    b.Navigation("Upgrade");
                });

            modelBuilder.Entity("dsstats.db.ReplayArcadeMatch", b =>
                {
                    b.HasOne("dsstats.db.ArcadeReplay", "ArcadeReplay")
                        .WithMany()
                        .HasForeignKey("ArcadeReplayId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("dsstats.db.Replay", "Replay")
                        .WithMany()
                        .HasForeignKey("ReplayId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("ArcadeReplay");

                    b.Navigation("Replay");
                });

            modelBuilder.Entity("dsstats.db.ReplayPlayer", b =>
                {
                    b.HasOne("dsstats.db.ReplayPlayer", "Opponent")
                        .WithMany()
                        .HasForeignKey("OpponentId")
                        .OnDelete(DeleteBehavior.SetNull);

                    b.HasOne("dsstats.db.Player", "Player")
                        .WithMany("ReplayPlayers")
                        .HasForeignKey("PlayerId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("dsstats.db.Replay", "Replay")
                        .WithMany("ReplayPlayers")
                        .HasForeignKey("ReplayId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Opponent");

                    b.Navigation("Player");

                    b.Navigation("Replay");
                });

            modelBuilder.Entity("dsstats.db.ReplayPlayerRating", b =>
                {
                    b.HasOne("dsstats.db.ReplayPlayer", "ReplayPlayer")
                        .WithMany("ReplayPlayerRatings")
                        .HasForeignKey("ReplayPlayerId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("ReplayPlayer");
                });

            modelBuilder.Entity("dsstats.db.ReplayRating", b =>
                {
                    b.HasOne("dsstats.db.Replay", "Replay")
                        .WithMany("ReplayRatings")
                        .HasForeignKey("ReplayId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Replay");
                });

            modelBuilder.Entity("dsstats.db.Spawn", b =>
                {
                    b.HasOne("dsstats.db.ReplayPlayer", "ReplayPlayer")
                        .WithMany("Spawns")
                        .HasForeignKey("ReplayPlayerId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("ReplayPlayer");
                });

            modelBuilder.Entity("dsstats.db.SpawnUnit", b =>
                {
                    b.HasOne("dsstats.db.Spawn", "Spawn")
                        .WithMany("SpawnUnits")
                        .HasForeignKey("SpawnId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("dsstats.db.Unit", "Unit")
                        .WithMany()
                        .HasForeignKey("UnitId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Spawn");

                    b.Navigation("Unit");
                });

            modelBuilder.Entity("dsstats.db.ArcadeReplay", b =>
                {
                    b.Navigation("ArcadeReplayPlayers");
                });

            modelBuilder.Entity("dsstats.db.Player", b =>
                {
                    b.Navigation("PlayerRatings");

                    b.Navigation("ReplayPlayers");
                });

            modelBuilder.Entity("dsstats.db.Replay", b =>
                {
                    b.Navigation("ReplayPlayers");

                    b.Navigation("ReplayRatings");
                });

            modelBuilder.Entity("dsstats.db.ReplayPlayer", b =>
                {
                    b.Navigation("PlayerUpgrades");

                    b.Navigation("ReplayPlayerRatings");

                    b.Navigation("Spawns");
                });

            modelBuilder.Entity("dsstats.db.Spawn", b =>
                {
                    b.Navigation("SpawnUnits");
                });
#pragma warning restore 612, 618
        }
    }
}
