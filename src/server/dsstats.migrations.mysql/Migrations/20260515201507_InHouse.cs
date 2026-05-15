using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dsstats.migrations.mysql.Migrations
{
    /// <inheritdoc />
    public partial class InHouse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ArcadeReplays",
                columns: table => new
                {
                    ArcadeReplayId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    RegionId = table.Column<int>(type: "int", nullable: false),
                    BnetBucketId = table.Column<long>(type: "bigint", nullable: false),
                    BnetRecordId = table.Column<long>(type: "bigint", nullable: false),
                    GameMode = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: false),
                    Duration = table.Column<int>(type: "int", nullable: false),
                    PlayerCount = table.Column<int>(type: "int", nullable: false),
                    WinnerTeam = table.Column<int>(type: "int", nullable: false),
                    Imported = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArcadeReplays", x => x.ArcadeReplayId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CombinedReplays",
                columns: table => new
                {
                    CombinedReplayId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ReplayId = table.Column<int>(type: "int", nullable: true),
                    ArcadeReplayId = table.Column<int>(type: "int", nullable: true),
                    GameMode = table.Column<int>(type: "int", nullable: false),
                    Duration = table.Column<int>(type: "int", nullable: false),
                    Gametime = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: false),
                    TE = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    PlayerCount = table.Column<int>(type: "int", nullable: false),
                    WinnerTeam = table.Column<int>(type: "int", nullable: false),
                    Imported = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CombinedReplays", x => x.CombinedReplayId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DsAbilities",
                columns: table => new
                {
                    DsAbilityId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Commander = table.Column<int>(type: "int", nullable: false),
                    Requirements = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Cooldown = table.Column<int>(type: "int", nullable: false),
                    GlobalTimer = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    EnergyCost = table.Column<float>(type: "float", nullable: false),
                    CastRange = table.Column<int>(type: "int", nullable: false),
                    AoeRadius = table.Column<float>(type: "float", nullable: false),
                    AbilityTarget = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "varchar(310)", maxLength: 310, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DsAbilities", x => x.DsAbilityId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DsUnits",
                columns: table => new
                {
                    DsUnitId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Commander = table.Column<int>(type: "int", nullable: false),
                    Tier = table.Column<int>(type: "int", nullable: false),
                    Cost = table.Column<int>(type: "int", nullable: false),
                    Life = table.Column<int>(type: "int", nullable: false),
                    Shields = table.Column<int>(type: "int", nullable: false),
                    Speed = table.Column<float>(type: "float", nullable: false),
                    Armor = table.Column<int>(type: "int", nullable: false),
                    ShieldArmor = table.Column<int>(type: "int", nullable: false),
                    StartingEnergy = table.Column<int>(type: "int", nullable: false),
                    MaxEnergy = table.Column<int>(type: "int", nullable: false),
                    HealthRegen = table.Column<float>(type: "float", nullable: false),
                    EnergyRegen = table.Column<float>(type: "float", nullable: false),
                    UnitType = table.Column<int>(type: "int", nullable: false),
                    UnitSize = table.Column<int>(type: "int", nullable: false),
                    MapUnitType = table.Column<int>(type: "int", nullable: false),
                    MovementType = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DsUnits", x => x.DsUnitId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "InHouseUsers",
                columns: table => new
                {
                    InHouseUserId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PublicId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    DisplayName = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsAdmin = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InHouseUsers", x => x.InHouseUserId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "MauiConfig",
                columns: table => new
                {
                    MauiConfigId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    AppGuid = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Version = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CPUCores = table.Column<int>(type: "int", nullable: false),
                    AutoDecode = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CheckForUpdates = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    UploadCredential = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ReplayStartName = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Culture = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UploadAskTime = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: false),
                    IgnoreReplays = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SessionWindowMode = table.Column<int>(type: "int", nullable: false),
                    SessionWindowHours = table.Column<int>(type: "int", nullable: false),
                    SessionWindowReplayCount = table.Column<int>(type: "int", nullable: false),
                    SessionWindowGameMode = table.Column<int>(type: "int", nullable: false),
                    SessionWindowInitialized = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MauiConfig", x => x.MauiConfigId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Players",
                columns: table => new
                {
                    PlayerId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ToonId_Region = table.Column<int>(type: "int", nullable: false),
                    ToonId_Realm = table.Column<int>(type: "int", nullable: false),
                    ToonId_Id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Players", x => x.PlayerId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ReplayArcadeMatches",
                columns: table => new
                {
                    ReplayArcadeMatchId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ReplayId = table.Column<int>(type: "int", nullable: false),
                    ArcadeReplayId = table.Column<int>(type: "int", nullable: false),
                    MatchTime = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReplayArcadeMatches", x => x.ReplayArcadeMatchId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ReplayIdResult",
                columns: table => new
                {
                    SeqKey = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ReplayIdsCsv = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Replays",
                columns: table => new
                {
                    ReplayId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    FileName = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Title = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Version = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    GameMode = table.Column<int>(type: "int", nullable: false),
                    RegionId = table.Column<int>(type: "int", nullable: false),
                    TE = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    PlayerCount = table.Column<int>(type: "int", nullable: false),
                    Gametime = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: false),
                    BaseBuild = table.Column<int>(type: "int", nullable: false),
                    Duration = table.Column<int>(type: "int", nullable: false),
                    Cannon = table.Column<int>(type: "int", nullable: false),
                    Bunker = table.Column<int>(type: "int", nullable: false),
                    WinnerTeam = table.Column<int>(type: "int", nullable: false),
                    MiddleChanges = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ReplayHash = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CompatHash = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Imported = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: false),
                    Uploaded = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Replays", x => x.ReplayId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ReplayUploadJobs",
                columns: table => new
                {
                    ReplayUploadJobId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Guid = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    BlobFilePath = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: false),
                    FinishedAt = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: true),
                    Error = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReplayUploadJobs", x => x.ReplayUploadJobId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Units",
                columns: table => new
                {
                    UnitId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Units", x => x.UnitId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Upgrades",
                columns: table => new
                {
                    UpgradeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Upgrades", x => x.UpgradeId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "UploadJobs",
                columns: table => new
                {
                    UploadJobId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PlayerIds = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Version = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BlobFilePath = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: false),
                    FinishedAt = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: true),
                    Error = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UploadJobs", x => x.UploadJobId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ArcadeReplayRatings",
                columns: table => new
                {
                    ArcadeReplayRatingId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ExpectedWinProbability = table.Column<int>(type: "int", nullable: false),
                    PlayerRatings = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PlayerRatingDeltas = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AvgRating = table.Column<int>(type: "int", nullable: false),
                    ArcadeReplayId = table.Column<int>(type: "int", nullable: false)
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DsAbilityDsUnit",
                columns: table => new
                {
                    AbilitiesDsAbilityId = table.Column<int>(type: "int", nullable: false),
                    DsUnitsDsUnitId = table.Column<int>(type: "int", nullable: false)
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DsUpgrades",
                columns: table => new
                {
                    DsUpgradeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Upgrade = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Commander = table.Column<int>(type: "int", nullable: false),
                    Cost = table.Column<int>(type: "int", nullable: false),
                    RequiredTier = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DsUnitId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DsUpgrades", x => x.DsUpgradeId);
                    table.ForeignKey(
                        name: "FK_DsUpgrades_DsUnits_DsUnitId",
                        column: x => x.DsUnitId,
                        principalTable: "DsUnits",
                        principalColumn: "DsUnitId");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DsWeapons",
                columns: table => new
                {
                    DsWeaponId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Range = table.Column<float>(type: "float", nullable: false),
                    AttackSpeed = table.Column<float>(type: "float", nullable: false),
                    Attacks = table.Column<int>(type: "int", nullable: false),
                    CanTarget = table.Column<int>(type: "int", nullable: false),
                    Damage = table.Column<int>(type: "int", nullable: false),
                    DamagePerUpgrade = table.Column<int>(type: "int", nullable: false),
                    DsUnitId = table.Column<int>(type: "int", nullable: false)
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "InHouseDeviceLinkCodes",
                columns: table => new
                {
                    InHouseDeviceLinkCodeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    InHouseUserId = table.Column<int>(type: "int", nullable: false),
                    CodeHash = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DisplayCode = table.Column<string>(type: "varchar(12)", maxLength: 12, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "InHouseGameSessions",
                columns: table => new
                {
                    InHouseGameSessionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PublicId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Name = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedByInHouseUserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: true),
                    ReplayIds = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "InHousePasskeyCredentials",
                columns: table => new
                {
                    InHousePasskeyCredentialId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    InHouseUserId = table.Column<int>(type: "int", nullable: false),
                    CredentialId = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UserHandle = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PublicKey = table.Column<byte[]>(type: "longblob", nullable: false),
                    SignatureCounter = table.Column<uint>(type: "int unsigned", nullable: false),
                    IsBackedUp = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DeviceName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "InHouseProfiles",
                columns: table => new
                {
                    InHouseProfileId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    InHouseUserId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ToonId_Region = table.Column<int>(type: "int", nullable: false),
                    ToonId_Realm = table.Column<int>(type: "int", nullable: false),
                    ToonId_Id = table.Column<int>(type: "int", nullable: false),
                    Active = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "InHouseSessions",
                columns: table => new
                {
                    InHouseSessionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    InHouseUserId = table.Column<int>(type: "int", nullable: false),
                    AccessTokenHash = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RefreshTokenHash = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    RefreshExpiresAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Sc2Profiles",
                columns: table => new
                {
                    Sc2ProfileId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Folder = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ToonId_Region = table.Column<int>(type: "int", nullable: false),
                    ToonId_Realm = table.Column<int>(type: "int", nullable: false),
                    ToonId_Id = table.Column<int>(type: "int", nullable: false),
                    Active = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    MauiConfigId = table.Column<int>(type: "int", nullable: false)
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ArcadeReplayPlayers",
                columns: table => new
                {
                    ArcadeReplayPlayerId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SlotNumber = table.Column<int>(type: "int", nullable: false),
                    Team = table.Column<int>(type: "int", nullable: false),
                    ArcadeReplayId = table.Column<int>(type: "int", nullable: false),
                    PlayerId = table.Column<int>(type: "int", nullable: false)
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PlayerRatings",
                columns: table => new
                {
                    PlayerRatingId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    RatingType = table.Column<int>(type: "int", nullable: false),
                    Games = table.Column<int>(type: "int", nullable: false),
                    ArcadeGames = table.Column<int>(type: "int", nullable: false),
                    DsstatsGames = table.Column<int>(type: "int", nullable: false),
                    Wins = table.Column<int>(type: "int", nullable: false),
                    Mvps = table.Column<int>(type: "int", nullable: false),
                    Main = table.Column<int>(type: "int", nullable: false),
                    MainCount = table.Column<int>(type: "int", nullable: false),
                    Change = table.Column<int>(type: "int", nullable: false),
                    Rating = table.Column<double>(type: "double", nullable: false),
                    Consistency = table.Column<double>(type: "double", nullable: false),
                    Confidence = table.Column<double>(type: "double", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    LastGame = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: false),
                    PlayerId = table.Column<int>(type: "int", nullable: false)
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ReplayObservers",
                columns: table => new
                {
                    ReplayObserversId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PlayerIds = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ReplayId = table.Column<int>(type: "int", nullable: false)
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ReplayPlayers",
                columns: table => new
                {
                    ReplayPlayerId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CompatHash = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Clan = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Race = table.Column<int>(type: "int", nullable: false),
                    SelectedRace = table.Column<int>(type: "int", nullable: false),
                    OppRace = table.Column<int>(type: "int", nullable: false),
                    TeamId = table.Column<int>(type: "int", nullable: false),
                    GamePos = table.Column<int>(type: "int", nullable: false),
                    Duration = table.Column<int>(type: "int", nullable: false),
                    Result = table.Column<int>(type: "int", nullable: false),
                    Apm = table.Column<int>(type: "int", nullable: false),
                    Messages = table.Column<int>(type: "int", nullable: false),
                    Pings = table.Column<int>(type: "int", nullable: false),
                    TierUpgrades = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Refineries = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsMvp = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsUploader = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ReplayId = table.Column<int>(type: "int", nullable: false),
                    PlayerId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReplayPlayers", x => x.ReplayPlayerId);
                    table.ForeignKey(
                        name: "FK_ReplayPlayers_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "PlayerId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReplayPlayers_Replays_ReplayId",
                        column: x => x.ReplayId,
                        principalTable: "Replays",
                        principalColumn: "ReplayId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ReplayRatings",
                columns: table => new
                {
                    ReplayRatingId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    RatingType = table.Column<int>(type: "int", nullable: false),
                    LeaverType = table.Column<int>(type: "int", nullable: false),
                    ExpectedWinProbability = table.Column<double>(type: "double", precision: 5, scale: 2, nullable: false),
                    IsPreRating = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    AvgRating = table.Column<int>(type: "int", nullable: false),
                    ReplayId = table.Column<int>(type: "int", nullable: false)
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BonusDamages",
                columns: table => new
                {
                    BonusDamageId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UnitType = table.Column<int>(type: "int", nullable: false),
                    Damage = table.Column<int>(type: "int", nullable: false),
                    PerUpgrade = table.Column<int>(type: "int", nullable: false),
                    DsWeaponId = table.Column<int>(type: "int", nullable: false)
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "InHouseGameSessionStateSnapshots",
                columns: table => new
                {
                    InHouseGameSessionId = table.Column<int>(type: "int", nullable: false),
                    Json = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InHouseGameSessionStateSnapshots", x => x.InHouseGameSessionId);
                    table.ForeignKey(
                        name: "FK_InHouseGameSessionStateSnapshots_InHouseGameSessions_InHouse~",
                        column: x => x.InHouseGameSessionId,
                        principalTable: "InHouseGameSessions",
                        principalColumn: "InHouseGameSessionId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PlayerUpgrades",
                columns: table => new
                {
                    PlayerUpgradeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Gameloop = table.Column<int>(type: "int", nullable: false),
                    UpgradeId = table.Column<int>(type: "int", nullable: false),
                    ReplayPlayerId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerUpgrades", x => x.PlayerUpgradeId);
                    table.ForeignKey(
                        name: "FK_PlayerUpgrades_ReplayPlayers_ReplayPlayerId",
                        column: x => x.ReplayPlayerId,
                        principalTable: "ReplayPlayers",
                        principalColumn: "ReplayPlayerId");
                    table.ForeignKey(
                        name: "FK_PlayerUpgrades_Upgrades_UpgradeId",
                        column: x => x.UpgradeId,
                        principalTable: "Upgrades",
                        principalColumn: "UpgradeId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Spawns",
                columns: table => new
                {
                    SpawnId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Breakpoint = table.Column<int>(type: "int", nullable: false),
                    Income = table.Column<int>(type: "int", nullable: false),
                    GasCount = table.Column<int>(type: "int", nullable: false),
                    ArmyValue = table.Column<int>(type: "int", nullable: false),
                    KilledValue = table.Column<int>(type: "int", nullable: false),
                    UpgradeSpent = table.Column<int>(type: "int", nullable: false),
                    ReplayPlayerId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Spawns", x => x.SpawnId);
                    table.ForeignKey(
                        name: "FK_Spawns_ReplayPlayers_ReplayPlayerId",
                        column: x => x.ReplayPlayerId,
                        principalTable: "ReplayPlayers",
                        principalColumn: "ReplayPlayerId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ReplayPlayerRatings",
                columns: table => new
                {
                    ReplayPlayerRatingId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    RatingType = table.Column<int>(type: "int", nullable: false),
                    RatingBefore = table.Column<double>(type: "double", precision: 7, scale: 2, nullable: false),
                    RatingDelta = table.Column<double>(type: "double", precision: 7, scale: 2, nullable: false),
                    ExpectedDelta = table.Column<double>(type: "double", precision: 7, scale: 2, nullable: false),
                    Games = table.Column<int>(type: "int", nullable: false),
                    ReplayRatingId = table.Column<int>(type: "int", nullable: false),
                    ReplayPlayerId = table.Column<int>(type: "int", nullable: false),
                    PlayerId = table.Column<int>(type: "int", nullable: false)
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SpawnUnits",
                columns: table => new
                {
                    SpawnUnitId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Count = table.Column<int>(type: "int", nullable: false),
                    Positions = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UnitId = table.Column<int>(type: "int", nullable: false),
                    SpawnId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpawnUnits", x => x.SpawnUnitId);
                    table.ForeignKey(
                        name: "FK_SpawnUnits_Spawns_SpawnId",
                        column: x => x.SpawnId,
                        principalTable: "Spawns",
                        principalColumn: "SpawnId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SpawnUnits_Units_UnitId",
                        column: x => x.UnitId,
                        principalTable: "Units",
                        principalColumn: "UnitId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

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
                name: "IX_Players_Name",
                table: "Players",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Players_ToonId_Region_ToonId_Realm_ToonId_Id",
                table: "Players",
                columns: new[] { "ToonId_Region", "ToonId_Realm", "ToonId_Id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlayerUpgrades_ReplayPlayerId",
                table: "PlayerUpgrades",
                column: "ReplayPlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerUpgrades_UpgradeId",
                table: "PlayerUpgrades",
                column: "UpgradeId");

            migrationBuilder.CreateIndex(
                name: "IX_ReplayObservers_ReplayId",
                table: "ReplayObservers",
                column: "ReplayId",
                unique: true);

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
                name: "IX_ReplayPlayers_PlayerId",
                table: "ReplayPlayers",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_ReplayPlayers_ReplayId",
                table: "ReplayPlayers",
                column: "ReplayId");

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
                name: "IX_Replays_CompatHash",
                table: "Replays",
                column: "CompatHash");

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
                name: "IX_Replays_ReplayHash",
                table: "Replays",
                column: "ReplayHash",
                unique: true);

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
                name: "IX_Spawns_ReplayPlayerId",
                table: "Spawns",
                column: "ReplayPlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_SpawnUnits_SpawnId",
                table: "SpawnUnits",
                column: "SpawnId");

            migrationBuilder.CreateIndex(
                name: "IX_SpawnUnits_UnitId",
                table: "SpawnUnits",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_Units_Name",
                table: "Units",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Upgrades_Name",
                table: "Upgrades",
                column: "Name",
                unique: true);

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
                name: "PlayerUpgrades");

            migrationBuilder.DropTable(
                name: "ReplayArcadeMatches");

            migrationBuilder.DropTable(
                name: "ReplayIdResult");

            migrationBuilder.DropTable(
                name: "ReplayObservers");

            migrationBuilder.DropTable(
                name: "ReplayPlayerRatings");

            migrationBuilder.DropTable(
                name: "ReplayUploadJobs");

            migrationBuilder.DropTable(
                name: "Sc2Profiles");

            migrationBuilder.DropTable(
                name: "SpawnUnits");

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
                name: "Upgrades");

            migrationBuilder.DropTable(
                name: "ReplayRatings");

            migrationBuilder.DropTable(
                name: "MauiConfig");

            migrationBuilder.DropTable(
                name: "Spawns");

            migrationBuilder.DropTable(
                name: "Units");

            migrationBuilder.DropTable(
                name: "DsUnits");

            migrationBuilder.DropTable(
                name: "InHouseUsers");

            migrationBuilder.DropTable(
                name: "ReplayPlayers");

            migrationBuilder.DropTable(
                name: "Players");

            migrationBuilder.DropTable(
                name: "Replays");
        }
    }
}
