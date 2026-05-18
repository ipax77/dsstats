using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace dsstats.migrations.postgresql.Migrations
{
    /// <inheritdoc />
    public partial class ReplayBuildDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CompatHash",
                table: "Replays",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FileName",
                table: "Replays",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Imported",
                table: "Replays",
                type: "timestamp(0) with time zone",
                precision: 0,
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "RegionId",
                table: "Replays",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "TE",
                table: "Replays",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Uploaded",
                table: "Replays",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "CompatHash",
                table: "ReplayPlayers",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsMvp",
                table: "ReplayPlayers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsUploader",
                table: "ReplayPlayers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "OppRace",
                table: "ReplayPlayers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int[]>(
                name: "Refineries",
                table: "ReplayPlayers",
                type: "integer[]",
                nullable: false,
                defaultValue: new int[0]);

            migrationBuilder.CreateTable(
                name: "ArcadeReplays",
                columns: table => new
                {
                    ArcadeReplayId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RegionId = table.Column<int>(type: "integer", nullable: false),
                    BnetBucketId = table.Column<long>(type: "bigint", nullable: false),
                    BnetRecordId = table.Column<long>(type: "bigint", nullable: false),
                    GameMode = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp(0) with time zone", precision: 0, nullable: false),
                    Duration = table.Column<int>(type: "integer", nullable: false),
                    PlayerCount = table.Column<int>(type: "integer", nullable: false),
                    WinnerTeam = table.Column<int>(type: "integer", nullable: false),
                    Imported = table.Column<DateTime>(type: "timestamp(0) with time zone", precision: 0, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArcadeReplays", x => x.ArcadeReplayId);
                });

            migrationBuilder.CreateTable(
                name: "CombinedReplays",
                columns: table => new
                {
                    CombinedReplayId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ReplayId = table.Column<int>(type: "integer", nullable: true),
                    ArcadeReplayId = table.Column<int>(type: "integer", nullable: true),
                    GameMode = table.Column<int>(type: "integer", nullable: false),
                    Duration = table.Column<int>(type: "integer", nullable: false),
                    Gametime = table.Column<DateTime>(type: "timestamp(0) with time zone", precision: 0, nullable: false),
                    TE = table.Column<bool>(type: "boolean", nullable: false),
                    PlayerCount = table.Column<int>(type: "integer", nullable: false),
                    WinnerTeam = table.Column<int>(type: "integer", nullable: false),
                    Imported = table.Column<DateTime>(type: "timestamp(0) with time zone", precision: 0, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CombinedReplays", x => x.CombinedReplayId);
                });

            migrationBuilder.CreateTable(
                name: "DsAbilities",
                columns: table => new
                {
                    DsAbilityId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Commander = table.Column<int>(type: "integer", nullable: false),
                    Requirements = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Cooldown = table.Column<int>(type: "integer", nullable: false),
                    GlobalTimer = table.Column<bool>(type: "boolean", nullable: false),
                    EnergyCost = table.Column<float>(type: "real", nullable: false),
                    CastRange = table.Column<int>(type: "integer", nullable: false),
                    AoeRadius = table.Column<float>(type: "real", nullable: false),
                    AbilityTarget = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(310)", maxLength: 310, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DsAbilities", x => x.DsAbilityId);
                });

            migrationBuilder.CreateTable(
                name: "DsUnits",
                columns: table => new
                {
                    DsUnitId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Commander = table.Column<int>(type: "integer", nullable: false),
                    Tier = table.Column<int>(type: "integer", nullable: false),
                    Cost = table.Column<int>(type: "integer", nullable: false),
                    Life = table.Column<int>(type: "integer", nullable: false),
                    Shields = table.Column<int>(type: "integer", nullable: false),
                    Speed = table.Column<float>(type: "real", nullable: false),
                    Armor = table.Column<int>(type: "integer", nullable: false),
                    ShieldArmor = table.Column<int>(type: "integer", nullable: false),
                    StartingEnergy = table.Column<int>(type: "integer", nullable: false),
                    MaxEnergy = table.Column<int>(type: "integer", nullable: false),
                    HealthRegen = table.Column<float>(type: "real", nullable: false),
                    EnergyRegen = table.Column<float>(type: "real", nullable: false),
                    UnitType = table.Column<int>(type: "integer", nullable: false),
                    UnitSize = table.Column<int>(type: "integer", nullable: false),
                    MapUnitType = table.Column<int>(type: "integer", nullable: false),
                    MovementType = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DsUnits", x => x.DsUnitId);
                });

            migrationBuilder.CreateTable(
                name: "InHouseUsers",
                columns: table => new
                {
                    InHouseUserId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    IsAdmin = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InHouseUsers", x => x.InHouseUserId);
                });

            migrationBuilder.CreateTable(
                name: "MauiConfig",
                columns: table => new
                {
                    MauiConfigId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AppGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    CPUCores = table.Column<int>(type: "integer", nullable: false),
                    AutoDecode = table.Column<bool>(type: "boolean", nullable: false),
                    CheckForUpdates = table.Column<bool>(type: "boolean", nullable: false),
                    UploadCredential = table.Column<bool>(type: "boolean", nullable: false),
                    ReplayStartName = table.Column<string>(type: "text", nullable: false),
                    Culture = table.Column<string>(type: "text", nullable: false),
                    UploadAskTime = table.Column<DateTime>(type: "timestamp(0) with time zone", precision: 0, nullable: false),
                    IgnoreReplays = table.Column<string[]>(type: "text[]", nullable: false),
                    SessionWindowMode = table.Column<int>(type: "integer", nullable: false),
                    SessionWindowHours = table.Column<int>(type: "integer", nullable: false),
                    SessionWindowReplayCount = table.Column<int>(type: "integer", nullable: false),
                    SessionWindowGameMode = table.Column<int>(type: "integer", nullable: false),
                    SessionWindowInitialized = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MauiConfig", x => x.MauiConfigId);
                });

            migrationBuilder.CreateTable(
                name: "PlayerRatings",
                columns: table => new
                {
                    PlayerRatingId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RatingType = table.Column<int>(type: "integer", nullable: false),
                    Games = table.Column<int>(type: "integer", nullable: false),
                    ArcadeGames = table.Column<int>(type: "integer", nullable: false),
                    DsstatsGames = table.Column<int>(type: "integer", nullable: false),
                    Wins = table.Column<int>(type: "integer", nullable: false),
                    Mvps = table.Column<int>(type: "integer", nullable: false),
                    Main = table.Column<int>(type: "integer", nullable: false),
                    MainCount = table.Column<int>(type: "integer", nullable: false),
                    Change = table.Column<int>(type: "integer", nullable: false),
                    Rating = table.Column<double>(type: "double precision", nullable: false),
                    Consistency = table.Column<double>(type: "double precision", nullable: false),
                    Confidence = table.Column<double>(type: "double precision", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    LastGame = table.Column<DateTime>(type: "timestamp(0) with time zone", precision: 0, nullable: false),
                    PlayerId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerRatings", x => x.PlayerRatingId);
                    table.ForeignKey(
                        name: "FK_PlayerRatings_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "PlayerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReplayArcadeMatches",
                columns: table => new
                {
                    ReplayArcadeMatchId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ReplayId = table.Column<int>(type: "integer", nullable: false),
                    ArcadeReplayId = table.Column<int>(type: "integer", nullable: false),
                    MatchTime = table.Column<DateTime>(type: "timestamp(0) with time zone", precision: 0, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReplayArcadeMatches", x => x.ReplayArcadeMatchId);
                });

            migrationBuilder.CreateTable(
                name: "ReplayBuildDetails",
                columns: table => new
                {
                    ReplayBuildDetailId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DetectionVersion = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp(0) with time zone", precision: 0, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp(0) with time zone", precision: 0, nullable: false),
                    FailureReason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ReplayId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReplayBuildDetails", x => x.ReplayBuildDetailId);
                    table.ForeignKey(
                        name: "FK_ReplayBuildDetails_Replays_ReplayId",
                        column: x => x.ReplayId,
                        principalTable: "Replays",
                        principalColumn: "ReplayId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReplayIdResult",
                columns: table => new
                {
                    SeqKey = table.Column<string>(type: "text", nullable: false),
                    ReplayIdsCsv = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "ReplayObservers",
                columns: table => new
                {
                    ReplayObserversId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlayerIds = table.Column<int[]>(type: "integer[]", nullable: true),
                    ReplayId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReplayObservers", x => x.ReplayObserversId);
                    table.ForeignKey(
                        name: "FK_ReplayObservers_Replays_ReplayId",
                        column: x => x.ReplayId,
                        principalTable: "Replays",
                        principalColumn: "ReplayId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReplayRatings",
                columns: table => new
                {
                    ReplayRatingId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RatingType = table.Column<int>(type: "integer", nullable: false),
                    LeaverType = table.Column<int>(type: "integer", nullable: false),
                    ExpectedWinProbability = table.Column<double>(type: "double precision", precision: 5, scale: 2, nullable: false),
                    IsPreRating = table.Column<bool>(type: "boolean", nullable: false),
                    AvgRating = table.Column<int>(type: "integer", nullable: false),
                    ReplayId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReplayRatings", x => x.ReplayRatingId);
                    table.ForeignKey(
                        name: "FK_ReplayRatings_Replays_ReplayId",
                        column: x => x.ReplayId,
                        principalTable: "Replays",
                        principalColumn: "ReplayId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReplayUploadJobs",
                columns: table => new
                {
                    ReplayUploadJobId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Guid = table.Column<Guid>(type: "uuid", nullable: false),
                    BlobFilePath = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp(0) with time zone", precision: 0, nullable: false),
                    FinishedAt = table.Column<DateTime>(type: "timestamp(0) with time zone", precision: 0, nullable: true),
                    Error = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReplayUploadJobs", x => x.ReplayUploadJobId);
                });

            migrationBuilder.CreateTable(
                name: "UploadJobs",
                columns: table => new
                {
                    UploadJobId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlayerIds = table.Column<int[]>(type: "integer[]", nullable: false),
                    Version = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    BlobFilePath = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp(0) with time zone", precision: 0, nullable: false),
                    FinishedAt = table.Column<DateTime>(type: "timestamp(0) with time zone", precision: 0, nullable: true),
                    Error = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UploadJobs", x => x.UploadJobId);
                });

            migrationBuilder.CreateTable(
                name: "ArcadeReplayPlayers",
                columns: table => new
                {
                    ArcadeReplayPlayerId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SlotNumber = table.Column<int>(type: "integer", nullable: false),
                    Team = table.Column<int>(type: "integer", nullable: false),
                    ArcadeReplayId = table.Column<int>(type: "integer", nullable: false),
                    PlayerId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArcadeReplayPlayers", x => x.ArcadeReplayPlayerId);
                    table.ForeignKey(
                        name: "FK_ArcadeReplayPlayers_ArcadeReplays_ArcadeReplayId",
                        column: x => x.ArcadeReplayId,
                        principalTable: "ArcadeReplays",
                        principalColumn: "ArcadeReplayId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ArcadeReplayPlayers_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "PlayerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ArcadeReplayRatings",
                columns: table => new
                {
                    ArcadeReplayRatingId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ExpectedWinProbability = table.Column<int>(type: "integer", nullable: false),
                    PlayerRatings = table.Column<int[]>(type: "integer[]", nullable: false),
                    PlayerRatingDeltas = table.Column<int[]>(type: "integer[]", nullable: false),
                    AvgRating = table.Column<int>(type: "integer", nullable: false),
                    ArcadeReplayId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArcadeReplayRatings", x => x.ArcadeReplayRatingId);
                    table.ForeignKey(
                        name: "FK_ArcadeReplayRatings_ArcadeReplays_ArcadeReplayId",
                        column: x => x.ArcadeReplayId,
                        principalTable: "ArcadeReplays",
                        principalColumn: "ArcadeReplayId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DsAbilityDsUnit",
                columns: table => new
                {
                    AbilitiesDsAbilityId = table.Column<int>(type: "integer", nullable: false),
                    DsUnitsDsUnitId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DsAbilityDsUnit", x => new { x.AbilitiesDsAbilityId, x.DsUnitsDsUnitId });
                    table.ForeignKey(
                        name: "FK_DsAbilityDsUnit_DsAbilities_AbilitiesDsAbilityId",
                        column: x => x.AbilitiesDsAbilityId,
                        principalTable: "DsAbilities",
                        principalColumn: "DsAbilityId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DsAbilityDsUnit_DsUnits_DsUnitsDsUnitId",
                        column: x => x.DsUnitsDsUnitId,
                        principalTable: "DsUnits",
                        principalColumn: "DsUnitId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DsUpgrades",
                columns: table => new
                {
                    DsUpgradeId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Upgrade = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Commander = table.Column<int>(type: "integer", nullable: false),
                    Cost = table.Column<int>(type: "integer", nullable: false),
                    RequiredTier = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    DsUnitId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DsUpgrades", x => x.DsUpgradeId);
                    table.ForeignKey(
                        name: "FK_DsUpgrades_DsUnits_DsUnitId",
                        column: x => x.DsUnitId,
                        principalTable: "DsUnits",
                        principalColumn: "DsUnitId");
                });

            migrationBuilder.CreateTable(
                name: "DsWeapons",
                columns: table => new
                {
                    DsWeaponId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Range = table.Column<float>(type: "real", nullable: false),
                    AttackSpeed = table.Column<float>(type: "real", nullable: false),
                    Attacks = table.Column<int>(type: "integer", nullable: false),
                    CanTarget = table.Column<int>(type: "integer", nullable: false),
                    Damage = table.Column<int>(type: "integer", nullable: false),
                    DamagePerUpgrade = table.Column<int>(type: "integer", nullable: false),
                    DsUnitId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DsWeapons", x => x.DsWeaponId);
                    table.ForeignKey(
                        name: "FK_DsWeapons_DsUnits_DsUnitId",
                        column: x => x.DsUnitId,
                        principalTable: "DsUnits",
                        principalColumn: "DsUnitId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InHouseDeviceLinkCodes",
                columns: table => new
                {
                    InHouseDeviceLinkCodeId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InHouseUserId = table.Column<int>(type: "integer", nullable: false),
                    CodeHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    DisplayCode = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InHouseDeviceLinkCodes", x => x.InHouseDeviceLinkCodeId);
                    table.ForeignKey(
                        name: "FK_InHouseDeviceLinkCodes_InHouseUsers_InHouseUserId",
                        column: x => x.InHouseUserId,
                        principalTable: "InHouseUsers",
                        principalColumn: "InHouseUserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InHouseGameSessions",
                columns: table => new
                {
                    InHouseGameSessionId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    CreatedByInHouseUserId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp(0) with time zone", precision: 0, nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "timestamp(0) with time zone", precision: 0, nullable: true),
                    ReplayIds = table.Column<int[]>(type: "integer[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InHouseGameSessions", x => x.InHouseGameSessionId);
                    table.ForeignKey(
                        name: "FK_InHouseGameSessions_InHouseUsers_CreatedByInHouseUserId",
                        column: x => x.CreatedByInHouseUserId,
                        principalTable: "InHouseUsers",
                        principalColumn: "InHouseUserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InHousePasskeyCredentials",
                columns: table => new
                {
                    InHousePasskeyCredentialId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InHouseUserId = table.Column<int>(type: "integer", nullable: false),
                    CredentialId = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    UserHandle = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    PublicKey = table.Column<byte[]>(type: "bytea", nullable: false),
                    SignatureCounter = table.Column<long>(type: "bigint", nullable: false),
                    IsBackedUp = table.Column<bool>(type: "boolean", nullable: false),
                    DeviceName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InHousePasskeyCredentials", x => x.InHousePasskeyCredentialId);
                    table.ForeignKey(
                        name: "FK_InHousePasskeyCredentials_InHouseUsers_InHouseUserId",
                        column: x => x.InHouseUserId,
                        principalTable: "InHouseUsers",
                        principalColumn: "InHouseUserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InHouseProfiles",
                columns: table => new
                {
                    InHouseProfileId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InHouseUserId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ToonId_Region = table.Column<int>(type: "integer", nullable: false),
                    ToonId_Realm = table.Column<int>(type: "integer", nullable: false),
                    ToonId_Id = table.Column<int>(type: "integer", nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InHouseProfiles", x => x.InHouseProfileId);
                    table.ForeignKey(
                        name: "FK_InHouseProfiles_InHouseUsers_InHouseUserId",
                        column: x => x.InHouseUserId,
                        principalTable: "InHouseUsers",
                        principalColumn: "InHouseUserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InHouseSessions",
                columns: table => new
                {
                    InHouseSessionId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InHouseUserId = table.Column<int>(type: "integer", nullable: false),
                    AccessTokenHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    RefreshTokenHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RefreshExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InHouseSessions", x => x.InHouseSessionId);
                    table.ForeignKey(
                        name: "FK_InHouseSessions_InHouseUsers_InHouseUserId",
                        column: x => x.InHouseUserId,
                        principalTable: "InHouseUsers",
                        principalColumn: "InHouseUserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Sc2Profiles",
                columns: table => new
                {
                    Sc2ProfileId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Folder = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ToonId_Region = table.Column<int>(type: "integer", nullable: false),
                    ToonId_Realm = table.Column<int>(type: "integer", nullable: false),
                    ToonId_Id = table.Column<int>(type: "integer", nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    MauiConfigId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sc2Profiles", x => x.Sc2ProfileId);
                    table.ForeignKey(
                        name: "FK_Sc2Profiles_MauiConfig_MauiConfigId",
                        column: x => x.MauiConfigId,
                        principalTable: "MauiConfig",
                        principalColumn: "MauiConfigId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReplayPlayerBuildDetails",
                columns: table => new
                {
                    ReplayPlayerBuildDetailId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GamePos = table.Column<int>(type: "integer", nullable: false),
                    TeamId = table.Column<int>(type: "integer", nullable: false),
                    Commander = table.Column<int>(type: "integer", nullable: false),
                    Build = table.Column<int>(type: "integer", nullable: false),
                    GasFirst = table.Column<bool>(type: "boolean", nullable: false),
                    Lane = table.Column<int>(type: "integer", nullable: false),
                    OppGamePos = table.Column<int>(type: "integer", nullable: false),
                    OppCommander = table.Column<int>(type: "integer", nullable: false),
                    OppBuild = table.Column<int>(type: "integer", nullable: false),
                    OppGasFirst = table.Column<bool>(type: "boolean", nullable: false),
                    Won = table.Column<bool>(type: "boolean", nullable: false),
                    ReplayBuildDetailId = table.Column<int>(type: "integer", nullable: false),
                    ReplayPlayerId = table.Column<int>(type: "integer", nullable: false),
                    OppReplayPlayerId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReplayPlayerBuildDetails", x => x.ReplayPlayerBuildDetailId);
                    table.ForeignKey(
                        name: "FK_ReplayPlayerBuildDetails_ReplayBuildDetails_ReplayBuildDeta~",
                        column: x => x.ReplayBuildDetailId,
                        principalTable: "ReplayBuildDetails",
                        principalColumn: "ReplayBuildDetailId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReplayPlayerBuildDetails_ReplayPlayers_OppReplayPlayerId",
                        column: x => x.OppReplayPlayerId,
                        principalTable: "ReplayPlayers",
                        principalColumn: "ReplayPlayerId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReplayPlayerBuildDetails_ReplayPlayers_ReplayPlayerId",
                        column: x => x.ReplayPlayerId,
                        principalTable: "ReplayPlayers",
                        principalColumn: "ReplayPlayerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReplayTeamBuildDetails",
                columns: table => new
                {
                    ReplayTeamBuildDetailId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TeamId = table.Column<int>(type: "integer", nullable: false),
                    TeamBuild = table.Column<int>(type: "integer", nullable: false),
                    ReplayBuildDetailId = table.Column<int>(type: "integer", nullable: false),
                    LeaderReplayPlayerId = table.Column<int>(type: "integer", nullable: false),
                    FollowerReplayPlayerId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReplayTeamBuildDetails", x => x.ReplayTeamBuildDetailId);
                    table.ForeignKey(
                        name: "FK_ReplayTeamBuildDetails_ReplayBuildDetails_ReplayBuildDetail~",
                        column: x => x.ReplayBuildDetailId,
                        principalTable: "ReplayBuildDetails",
                        principalColumn: "ReplayBuildDetailId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReplayTeamBuildDetails_ReplayPlayers_FollowerReplayPlayerId",
                        column: x => x.FollowerReplayPlayerId,
                        principalTable: "ReplayPlayers",
                        principalColumn: "ReplayPlayerId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReplayTeamBuildDetails_ReplayPlayers_LeaderReplayPlayerId",
                        column: x => x.LeaderReplayPlayerId,
                        principalTable: "ReplayPlayers",
                        principalColumn: "ReplayPlayerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReplayPlayerRatings",
                columns: table => new
                {
                    ReplayPlayerRatingId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RatingType = table.Column<int>(type: "integer", nullable: false),
                    RatingBefore = table.Column<double>(type: "double precision", precision: 7, scale: 2, nullable: false),
                    RatingDelta = table.Column<double>(type: "double precision", precision: 7, scale: 2, nullable: false),
                    ExpectedDelta = table.Column<double>(type: "double precision", precision: 7, scale: 2, nullable: false),
                    Games = table.Column<int>(type: "integer", nullable: false),
                    ReplayRatingId = table.Column<int>(type: "integer", nullable: false),
                    ReplayPlayerId = table.Column<int>(type: "integer", nullable: false),
                    PlayerId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReplayPlayerRatings", x => x.ReplayPlayerRatingId);
                    table.ForeignKey(
                        name: "FK_ReplayPlayerRatings_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "PlayerId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReplayPlayerRatings_ReplayPlayers_ReplayPlayerId",
                        column: x => x.ReplayPlayerId,
                        principalTable: "ReplayPlayers",
                        principalColumn: "ReplayPlayerId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReplayPlayerRatings_ReplayRatings_ReplayRatingId",
                        column: x => x.ReplayRatingId,
                        principalTable: "ReplayRatings",
                        principalColumn: "ReplayRatingId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BonusDamages",
                columns: table => new
                {
                    BonusDamageId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UnitType = table.Column<int>(type: "integer", nullable: false),
                    Damage = table.Column<int>(type: "integer", nullable: false),
                    PerUpgrade = table.Column<int>(type: "integer", nullable: false),
                    DsWeaponId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BonusDamages", x => x.BonusDamageId);
                    table.ForeignKey(
                        name: "FK_BonusDamages_DsWeapons_DsWeaponId",
                        column: x => x.DsWeaponId,
                        principalTable: "DsWeapons",
                        principalColumn: "DsWeaponId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InHouseGameSessionStateSnapshots",
                columns: table => new
                {
                    InHouseGameSessionId = table.Column<int>(type: "integer", nullable: false),
                    Json = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp(0) with time zone", precision: 0, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp(0) with time zone", precision: 0, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InHouseGameSessionStateSnapshots", x => x.InHouseGameSessionId);
                    table.ForeignKey(
                        name: "FK_InHouseGameSessionStateSnapshots_InHouseGameSessions_InHous~",
                        column: x => x.InHouseGameSessionId,
                        principalTable: "InHouseGameSessions",
                        principalColumn: "InHouseGameSessionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Replays_CompatHash",
                table: "Replays",
                column: "CompatHash");

            migrationBuilder.CreateIndex(
                name: "IX_Replays_GameMode_TE_PlayerCount_WinnerTeam_Duration_ReplayId",
                table: "Replays",
                columns: new[] { "GameMode", "TE", "PlayerCount", "WinnerTeam", "Duration", "ReplayId" });

            migrationBuilder.CreateIndex(
                name: "IX_Replays_Gametime",
                table: "Replays",
                column: "Gametime");

            migrationBuilder.CreateIndex(
                name: "IX_Replays_Gametime_Duration_WinnerTeam_PlayerCount_GameMode_TE",
                table: "Replays",
                columns: new[] { "Gametime", "Duration", "WinnerTeam", "PlayerCount", "GameMode", "TE" });

            migrationBuilder.CreateIndex(
                name: "IX_Replays_Gametime_ReplayId",
                table: "Replays",
                columns: new[] { "Gametime", "ReplayId" });

            migrationBuilder.CreateIndex(
                name: "IX_Players_Name",
                table: "Players",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_ArcadeReplayPlayers_ArcadeReplayId",
                table: "ArcadeReplayPlayers",
                column: "ArcadeReplayId");

            migrationBuilder.CreateIndex(
                name: "IX_ArcadeReplayPlayers_PlayerId",
                table: "ArcadeReplayPlayers",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_ArcadeReplayRatings_ArcadeReplayId",
                table: "ArcadeReplayRatings",
                column: "ArcadeReplayId");

            migrationBuilder.CreateIndex(
                name: "IX_ArcadeReplays_CreatedAt_ArcadeReplayId",
                table: "ArcadeReplays",
                columns: new[] { "CreatedAt", "ArcadeReplayId" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_ArcadeReplays_RegionId_BnetBucketId_BnetRecordId",
                table: "ArcadeReplays",
                columns: new[] { "RegionId", "BnetBucketId", "BnetRecordId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BonusDamages_DsWeaponId",
                table: "BonusDamages",
                column: "DsWeaponId");

            migrationBuilder.CreateIndex(
                name: "IX_CombinedReplays_ArcadeReplayId",
                table: "CombinedReplays",
                column: "ArcadeReplayId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CombinedReplays_Gametime",
                table: "CombinedReplays",
                column: "Gametime");

            migrationBuilder.CreateIndex(
                name: "IX_CombinedReplays_Imported",
                table: "CombinedReplays",
                column: "Imported");

            migrationBuilder.CreateIndex(
                name: "IX_CombinedReplays_ReplayId",
                table: "CombinedReplays",
                column: "ReplayId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DsAbilityDsUnit_DsUnitsDsUnitId",
                table: "DsAbilityDsUnit",
                column: "DsUnitsDsUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_DsUpgrades_DsUnitId",
                table: "DsUpgrades",
                column: "DsUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_DsWeapons_DsUnitId",
                table: "DsWeapons",
                column: "DsUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_InHouseDeviceLinkCodes_CodeHash",
                table: "InHouseDeviceLinkCodes",
                column: "CodeHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InHouseDeviceLinkCodes_ExpiresAt",
                table: "InHouseDeviceLinkCodes",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_InHouseDeviceLinkCodes_InHouseUserId",
                table: "InHouseDeviceLinkCodes",
                column: "InHouseUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InHouseGameSessions_ClosedAt",
                table: "InHouseGameSessions",
                column: "ClosedAt");

            migrationBuilder.CreateIndex(
                name: "IX_InHouseGameSessions_CreatedAt",
                table: "InHouseGameSessions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_InHouseGameSessions_CreatedByInHouseUserId",
                table: "InHouseGameSessions",
                column: "CreatedByInHouseUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InHouseGameSessions_PublicId",
                table: "InHouseGameSessions",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InHousePasskeyCredentials_CredentialId",
                table: "InHousePasskeyCredentials",
                column: "CredentialId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InHousePasskeyCredentials_InHouseUserId",
                table: "InHousePasskeyCredentials",
                column: "InHouseUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InHousePasskeyCredentials_UserHandle",
                table: "InHousePasskeyCredentials",
                column: "UserHandle");

            migrationBuilder.CreateIndex(
                name: "IX_InHouseProfiles_InHouseUserId",
                table: "InHouseProfiles",
                column: "InHouseUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InHouseProfiles_ToonId_Region_ToonId_Realm_ToonId_Id",
                table: "InHouseProfiles",
                columns: new[] { "ToonId_Region", "ToonId_Realm", "ToonId_Id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InHouseSessions_AccessTokenHash",
                table: "InHouseSessions",
                column: "AccessTokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InHouseSessions_ExpiresAt",
                table: "InHouseSessions",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_InHouseSessions_InHouseUserId",
                table: "InHouseSessions",
                column: "InHouseUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InHouseSessions_RefreshTokenHash",
                table: "InHouseSessions",
                column: "RefreshTokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InHouseUsers_DisplayName",
                table: "InHouseUsers",
                column: "DisplayName");

            migrationBuilder.CreateIndex(
                name: "IX_InHouseUsers_PublicId",
                table: "InHouseUsers",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlayerRatings_LastGame",
                table: "PlayerRatings",
                column: "LastGame");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerRatings_PlayerId_RatingType",
                table: "PlayerRatings",
                columns: new[] { "PlayerId", "RatingType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlayerRatings_Rating",
                table: "PlayerRatings",
                column: "Rating");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerRatings_RatingType",
                table: "PlayerRatings",
                column: "RatingType");

            migrationBuilder.CreateIndex(
                name: "IX_ReplayBuildDetails_ReplayId",
                table: "ReplayBuildDetails",
                column: "ReplayId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReplayBuildDetails_Status_DetectionVersion",
                table: "ReplayBuildDetails",
                columns: new[] { "Status", "DetectionVersion" });

            migrationBuilder.CreateIndex(
                name: "IX_ReplayObservers_ReplayId",
                table: "ReplayObservers",
                column: "ReplayId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReplayPlayerBuildDetails_Commander_Build_OppCommander_OppBu~",
                table: "ReplayPlayerBuildDetails",
                columns: new[] { "Commander", "Build", "OppCommander", "OppBuild" });

            migrationBuilder.CreateIndex(
                name: "IX_ReplayPlayerBuildDetails_Commander_Build_TeamId_Won",
                table: "ReplayPlayerBuildDetails",
                columns: new[] { "Commander", "Build", "TeamId", "Won" });

            migrationBuilder.CreateIndex(
                name: "IX_ReplayPlayerBuildDetails_OppReplayPlayerId",
                table: "ReplayPlayerBuildDetails",
                column: "OppReplayPlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_ReplayPlayerBuildDetails_ReplayBuildDetailId",
                table: "ReplayPlayerBuildDetails",
                column: "ReplayBuildDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_ReplayPlayerBuildDetails_ReplayPlayerId",
                table: "ReplayPlayerBuildDetails",
                column: "ReplayPlayerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReplayPlayerBuildDetails_TeamId_Lane",
                table: "ReplayPlayerBuildDetails",
                columns: new[] { "TeamId", "Lane" });

            migrationBuilder.CreateIndex(
                name: "IX_ReplayPlayerRatings_PlayerId",
                table: "ReplayPlayerRatings",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_ReplayPlayerRatings_ReplayPlayerId",
                table: "ReplayPlayerRatings",
                column: "ReplayPlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_ReplayPlayerRatings_ReplayRatingId_ReplayPlayerId_RatingType",
                table: "ReplayPlayerRatings",
                columns: new[] { "ReplayRatingId", "ReplayPlayerId", "RatingType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReplayRatings_IsPreRating",
                table: "ReplayRatings",
                column: "IsPreRating");

            migrationBuilder.CreateIndex(
                name: "IX_ReplayRatings_ReplayId_RatingType",
                table: "ReplayRatings",
                columns: new[] { "ReplayId", "RatingType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReplayTeamBuildDetails_FollowerReplayPlayerId",
                table: "ReplayTeamBuildDetails",
                column: "FollowerReplayPlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_ReplayTeamBuildDetails_LeaderReplayPlayerId",
                table: "ReplayTeamBuildDetails",
                column: "LeaderReplayPlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_ReplayTeamBuildDetails_ReplayBuildDetailId",
                table: "ReplayTeamBuildDetails",
                column: "ReplayBuildDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_ReplayTeamBuildDetails_TeamBuild_TeamId",
                table: "ReplayTeamBuildDetails",
                columns: new[] { "TeamBuild", "TeamId" });

            migrationBuilder.CreateIndex(
                name: "IX_ReplayUploadJobs_CreatedAt",
                table: "ReplayUploadJobs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ReplayUploadJobs_FinishedAt",
                table: "ReplayUploadJobs",
                column: "FinishedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Sc2Profiles_MauiConfigId",
                table: "Sc2Profiles",
                column: "MauiConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_Sc2Profiles_Name",
                table: "Sc2Profiles",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Sc2Profiles_ToonId_Region_ToonId_Realm_ToonId_Id",
                table: "Sc2Profiles",
                columns: new[] { "ToonId_Region", "ToonId_Realm", "ToonId_Id" });

            migrationBuilder.CreateIndex(
                name: "IX_UploadJobs_CreatedAt",
                table: "UploadJobs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_UploadJobs_FinishedAt",
                table: "UploadJobs",
                column: "FinishedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ArcadeReplayPlayers");

            migrationBuilder.DropTable(
                name: "ArcadeReplayRatings");

            migrationBuilder.DropTable(
                name: "BonusDamages");

            migrationBuilder.DropTable(
                name: "CombinedReplays");

            migrationBuilder.DropTable(
                name: "DsAbilityDsUnit");

            migrationBuilder.DropTable(
                name: "DsUpgrades");

            migrationBuilder.DropTable(
                name: "InHouseDeviceLinkCodes");

            migrationBuilder.DropTable(
                name: "InHouseGameSessionStateSnapshots");

            migrationBuilder.DropTable(
                name: "InHousePasskeyCredentials");

            migrationBuilder.DropTable(
                name: "InHouseProfiles");

            migrationBuilder.DropTable(
                name: "InHouseSessions");

            migrationBuilder.DropTable(
                name: "PlayerRatings");

            migrationBuilder.DropTable(
                name: "ReplayArcadeMatches");

            migrationBuilder.DropTable(
                name: "ReplayIdResult");

            migrationBuilder.DropTable(
                name: "ReplayObservers");

            migrationBuilder.DropTable(
                name: "ReplayPlayerBuildDetails");

            migrationBuilder.DropTable(
                name: "ReplayPlayerRatings");

            migrationBuilder.DropTable(
                name: "ReplayTeamBuildDetails");

            migrationBuilder.DropTable(
                name: "ReplayUploadJobs");

            migrationBuilder.DropTable(
                name: "Sc2Profiles");

            migrationBuilder.DropTable(
                name: "UploadJobs");

            migrationBuilder.DropTable(
                name: "ArcadeReplays");

            migrationBuilder.DropTable(
                name: "DsWeapons");

            migrationBuilder.DropTable(
                name: "DsAbilities");

            migrationBuilder.DropTable(
                name: "InHouseGameSessions");

            migrationBuilder.DropTable(
                name: "ReplayRatings");

            migrationBuilder.DropTable(
                name: "ReplayBuildDetails");

            migrationBuilder.DropTable(
                name: "MauiConfig");

            migrationBuilder.DropTable(
                name: "DsUnits");

            migrationBuilder.DropTable(
                name: "InHouseUsers");

            migrationBuilder.DropIndex(
                name: "IX_Replays_CompatHash",
                table: "Replays");

            migrationBuilder.DropIndex(
                name: "IX_Replays_GameMode_TE_PlayerCount_WinnerTeam_Duration_ReplayId",
                table: "Replays");

            migrationBuilder.DropIndex(
                name: "IX_Replays_Gametime",
                table: "Replays");

            migrationBuilder.DropIndex(
                name: "IX_Replays_Gametime_Duration_WinnerTeam_PlayerCount_GameMode_TE",
                table: "Replays");

            migrationBuilder.DropIndex(
                name: "IX_Replays_Gametime_ReplayId",
                table: "Replays");

            migrationBuilder.DropIndex(
                name: "IX_Players_Name",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "CompatHash",
                table: "Replays");

            migrationBuilder.DropColumn(
                name: "FileName",
                table: "Replays");

            migrationBuilder.DropColumn(
                name: "Imported",
                table: "Replays");

            migrationBuilder.DropColumn(
                name: "RegionId",
                table: "Replays");

            migrationBuilder.DropColumn(
                name: "TE",
                table: "Replays");

            migrationBuilder.DropColumn(
                name: "Uploaded",
                table: "Replays");

            migrationBuilder.DropColumn(
                name: "CompatHash",
                table: "ReplayPlayers");

            migrationBuilder.DropColumn(
                name: "IsMvp",
                table: "ReplayPlayers");

            migrationBuilder.DropColumn(
                name: "IsUploader",
                table: "ReplayPlayers");

            migrationBuilder.DropColumn(
                name: "OppRace",
                table: "ReplayPlayers");

            migrationBuilder.DropColumn(
                name: "Refineries",
                table: "ReplayPlayers");
        }
    }
}
