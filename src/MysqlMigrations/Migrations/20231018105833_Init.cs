using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MysqlMigrations.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ArcadePlayers",
                columns: table => new
                {
                    ArcadePlayerId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RegionId = table.Column<int>(type: "int", nullable: false),
                    RealmId = table.Column<int>(type: "int", nullable: false),
                    ProfileId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArcadePlayers", x => x.ArcadePlayerId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

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
                    TournamentEdition = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    WinnerTeam = table.Column<int>(type: "int", nullable: false),
                    Imported = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: false),
                    ReplayHash = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArcadeReplays", x => x.ArcadeReplayId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CommanderMmrs",
                columns: table => new
                {
                    CommanderMmrId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Race = table.Column<int>(type: "int", nullable: false),
                    OppRace = table.Column<int>(type: "int", nullable: false),
                    SynergyMmr = table.Column<double>(type: "double", nullable: false),
                    AntiSynergyMmr = table.Column<double>(type: "double", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommanderMmrs", x => x.CommanderMmrId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DsUpdates",
                columns: table => new
                {
                    DsUpdateId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Commander = table.Column<int>(type: "int", nullable: false),
                    Time = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: false),
                    DiscordId = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Change = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DsUpdates", x => x.DsUpdateId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Events",
                columns: table => new
                {
                    EventId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EventGuid = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    EventStart = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: false),
                    WinnerTeam = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    GameMode = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Events", x => x.EventId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "FunStatMemories",
                columns: table => new
                {
                    FunStatsMemoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Created = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: false),
                    RatingType = table.Column<int>(type: "int", nullable: false),
                    TimePeriod = table.Column<int>(type: "int", nullable: false),
                    TotalTimePlayed = table.Column<long>(type: "bigint", nullable: false),
                    AvgGameDuration = table.Column<int>(type: "int", nullable: false),
                    UnitNameMost = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UnitCountMost = table.Column<int>(type: "int", nullable: false),
                    UnitNameLeast = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UnitCountLeast = table.Column<int>(type: "int", nullable: false),
                    FirstReplay = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    GreatestArmyReplay = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MostUpgradesReplay = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MostCompetitiveReplay = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    GreatestComebackReplay = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FunStatMemories", x => x.FunStatsMemoryId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ReplayDownloadCounts",
                columns: table => new
                {
                    ReplayDownloadCountId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ReplayHash = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReplayDownloadCounts", x => x.ReplayDownloadCountId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ReplayViewCounts",
                columns: table => new
                {
                    ReplayViewCountId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ReplayHash = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReplayViewCounts", x => x.ReplayViewCountId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SkipReplays",
                columns: table => new
                {
                    SkipReplayId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Path = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkipReplays", x => x.SkipReplayId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Units",
                columns: table => new
                {
                    UnitId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
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
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Cost = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Upgrades", x => x.UpgradeId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Uploaders",
                columns: table => new
                {
                    UploaderId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    AppGuid = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    AppVersion = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Identifier = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LatestUpload = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: false),
                    LatestReplay = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: false),
                    Games = table.Column<int>(type: "int", nullable: false),
                    Wins = table.Column<int>(type: "int", nullable: false),
                    Mvp = table.Column<int>(type: "int", nullable: false),
                    MainCommander = table.Column<int>(type: "int", nullable: false),
                    MainCount = table.Column<int>(type: "int", nullable: false),
                    TeamGames = table.Column<int>(type: "int", nullable: false),
                    UploadLastDisabled = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: false),
                    UploadDisabledCount = table.Column<int>(type: "int", nullable: false),
                    UploadIsDisabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Uploaders", x => x.UploaderId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ArcadePlayerRatings",
                columns: table => new
                {
                    ArcadePlayerRatingId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    RatingType = table.Column<int>(type: "int", nullable: false),
                    Rating = table.Column<double>(type: "double", nullable: false),
                    Pos = table.Column<int>(type: "int", nullable: false),
                    Games = table.Column<int>(type: "int", nullable: false),
                    Wins = table.Column<int>(type: "int", nullable: false),
                    Mvp = table.Column<int>(type: "int", nullable: false),
                    TeamGames = table.Column<int>(type: "int", nullable: false),
                    MainCount = table.Column<int>(type: "int", nullable: false),
                    Main = table.Column<int>(type: "int", nullable: false),
                    Consistency = table.Column<double>(type: "double", nullable: false),
                    Confidence = table.Column<double>(type: "double", nullable: false),
                    IsUploader = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ArcadePlayerId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArcadePlayerRatings", x => x.ArcadePlayerRatingId);
                    table.ForeignKey(
                        name: "FK_ArcadePlayerRatings_ArcadePlayers_ArcadePlayerId",
                        column: x => x.ArcadePlayerId,
                        principalTable: "ArcadePlayers",
                        principalColumn: "ArcadePlayerId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ArcadeReplayPlayers",
                columns: table => new
                {
                    ArcadeReplayPlayerId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SlotNumber = table.Column<int>(type: "int", nullable: false),
                    Team = table.Column<int>(type: "int", nullable: false),
                    Discriminator = table.Column<int>(type: "int", nullable: false),
                    PlayerResult = table.Column<int>(type: "int", nullable: false),
                    ArcadePlayerId = table.Column<int>(type: "int", nullable: false),
                    ArcadeReplayId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArcadeReplayPlayers", x => x.ArcadeReplayPlayerId);
                    table.ForeignKey(
                        name: "FK_ArcadeReplayPlayers_ArcadePlayers_ArcadePlayerId",
                        column: x => x.ArcadePlayerId,
                        principalTable: "ArcadePlayers",
                        principalColumn: "ArcadePlayerId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ArcadeReplayPlayers_ArcadeReplays_ArcadeReplayId",
                        column: x => x.ArcadeReplayId,
                        principalTable: "ArcadeReplays",
                        principalColumn: "ArcadeReplayId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ArcadeReplayRatings",
                columns: table => new
                {
                    ArcadeReplayRatingId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    RatingType = table.Column<int>(type: "int", nullable: false),
                    LeaverType = table.Column<int>(type: "int", nullable: false),
                    ExpectationToWin = table.Column<float>(type: "float", nullable: false),
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
                name: "ReplayEvents",
                columns: table => new
                {
                    ReplayEventId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Round = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    WinnerTeam = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RunnerTeam = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Ban1 = table.Column<int>(type: "int", nullable: false),
                    Ban2 = table.Column<int>(type: "int", nullable: false),
                    Ban3 = table.Column<int>(type: "int", nullable: false),
                    Ban4 = table.Column<int>(type: "int", nullable: false),
                    Ban5 = table.Column<int>(type: "int", nullable: false),
                    EventId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReplayEvents", x => x.ReplayEventId);
                    table.ForeignKey(
                        name: "FK_ReplayEvents_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "EventId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BattleNetInfos",
                columns: table => new
                {
                    BattleNetInfoId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    BattleNetId = table.Column<int>(type: "int", nullable: false),
                    UploaderId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BattleNetInfos", x => x.BattleNetInfoId);
                    table.ForeignKey(
                        name: "FK_BattleNetInfos_Uploaders_UploaderId",
                        column: x => x.UploaderId,
                        principalTable: "Uploaders",
                        principalColumn: "UploaderId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Players",
                columns: table => new
                {
                    PlayerId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ToonId = table.Column<int>(type: "int", nullable: false),
                    RegionId = table.Column<int>(type: "int", nullable: false),
                    RealmId = table.Column<int>(type: "int", nullable: false),
                    NotUploadCount = table.Column<int>(type: "int", nullable: false),
                    DisconnectCount = table.Column<int>(type: "int", nullable: false),
                    RageQuitCount = table.Column<int>(type: "int", nullable: false),
                    ArcadeDefeatsSinceLastUpload = table.Column<int>(type: "int", nullable: false),
                    UploaderId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Players", x => x.PlayerId);
                    table.ForeignKey(
                        name: "FK_Players_Uploaders_UploaderId",
                        column: x => x.UploaderId,
                        principalTable: "Uploaders",
                        principalColumn: "UploaderId");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ArcadePlayerRatingChanges",
                columns: table => new
                {
                    ArcadePlayerRatingChangeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Change24h = table.Column<float>(type: "float", nullable: false),
                    Change10d = table.Column<float>(type: "float", nullable: false),
                    Change30d = table.Column<float>(type: "float", nullable: false),
                    ArcadePlayerRatingId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArcadePlayerRatingChanges", x => x.ArcadePlayerRatingChangeId);
                    table.ForeignKey(
                        name: "FK_ArcadePlayerRatingChanges_ArcadePlayerRatings_ArcadePlayerRa~",
                        column: x => x.ArcadePlayerRatingId,
                        principalTable: "ArcadePlayerRatings",
                        principalColumn: "ArcadePlayerRatingId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ArcadeReplayPlayerRatings",
                columns: table => new
                {
                    ArcadeReplayPlayerRatingId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    GamePos = table.Column<int>(type: "int", nullable: false),
                    Rating = table.Column<float>(type: "float", nullable: false),
                    RatingChange = table.Column<float>(type: "float", nullable: false),
                    Games = table.Column<int>(type: "int", nullable: false),
                    Consistency = table.Column<float>(type: "float", nullable: false),
                    Confidence = table.Column<float>(type: "float", nullable: false),
                    ArcadeReplayPlayerId = table.Column<int>(type: "int", nullable: false),
                    ArcadeReplayRatingId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArcadeReplayPlayerRatings", x => x.ArcadeReplayPlayerRatingId);
                    table.ForeignKey(
                        name: "FK_ArcadeReplayPlayerRatings_ArcadeReplayPlayers_ArcadeReplayPl~",
                        column: x => x.ArcadeReplayPlayerId,
                        principalTable: "ArcadeReplayPlayers",
                        principalColumn: "ArcadeReplayPlayerId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ArcadeReplayPlayerRatings_ArcadeReplayRatings_ArcadeReplayRa~",
                        column: x => x.ArcadeReplayRatingId,
                        principalTable: "ArcadeReplayRatings",
                        principalColumn: "ArcadeReplayRatingId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Replays",
                columns: table => new
                {
                    ReplayId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    FileName = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Uploaded = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    TournamentEdition = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    GameTime = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: false),
                    Imported = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: true),
                    Duration = table.Column<int>(type: "int", nullable: false),
                    WinnerTeam = table.Column<int>(type: "int", nullable: false),
                    PlayerResult = table.Column<int>(type: "int", nullable: false),
                    PlayerPos = table.Column<int>(type: "int", nullable: false),
                    ResultCorrected = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    GameMode = table.Column<int>(type: "int", nullable: false),
                    Objective = table.Column<int>(type: "int", nullable: false),
                    Bunker = table.Column<int>(type: "int", nullable: false),
                    Cannon = table.Column<int>(type: "int", nullable: false),
                    Minkillsum = table.Column<int>(type: "int", nullable: false),
                    Maxkillsum = table.Column<int>(type: "int", nullable: false),
                    Minarmy = table.Column<int>(type: "int", nullable: false),
                    Minincome = table.Column<int>(type: "int", nullable: false),
                    Maxleaver = table.Column<int>(type: "int", nullable: false),
                    Playercount = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    ReplayHash = table.Column<string>(type: "char(64)", fixedLength: true, maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DefaultFilter = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Views = table.Column<int>(type: "int", nullable: false),
                    Downloads = table.Column<int>(type: "int", nullable: false),
                    Middle = table.Column<string>(type: "varchar(4000)", maxLength: 4000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CommandersTeam1 = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CommandersTeam2 = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ReplayEventId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Replays", x => x.ReplayId);
                    table.ForeignKey(
                        name: "FK_Replays_ReplayEvents_ReplayEventId",
                        column: x => x.ReplayEventId,
                        principalTable: "ReplayEvents",
                        principalColumn: "ReplayEventId");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ComboPlayerRatings",
                columns: table => new
                {
                    ComboPlayerRatingId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    RatingType = table.Column<int>(type: "int", nullable: false),
                    Games = table.Column<int>(type: "int", nullable: false),
                    Wins = table.Column<int>(type: "int", nullable: false),
                    Rating = table.Column<double>(type: "double", nullable: false),
                    Consistency = table.Column<double>(type: "double", nullable: false),
                    Confidence = table.Column<double>(type: "double", nullable: false),
                    Pos = table.Column<int>(type: "int", nullable: false),
                    PlayerId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComboPlayerRatings", x => x.ComboPlayerRatingId);
                    table.ForeignKey(
                        name: "FK_ComboPlayerRatings_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "PlayerId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "NoUploadResults",
                columns: table => new
                {
                    NoUploadResultId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TotalReplays = table.Column<int>(type: "int", nullable: false),
                    Created = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: false),
                    LatestReplay = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: false),
                    NoUploadTotal = table.Column<int>(type: "int", nullable: false),
                    NoUploadDefeats = table.Column<int>(type: "int", nullable: false),
                    LatestNoUpload = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: false),
                    LatestUpload = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: false),
                    PlayerId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NoUploadResults", x => x.NoUploadResultId);
                    table.ForeignKey(
                        name: "FK_NoUploadResults_Players_PlayerId",
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
                    Rating = table.Column<double>(type: "double", nullable: false),
                    Games = table.Column<int>(type: "int", nullable: false),
                    Wins = table.Column<int>(type: "int", nullable: false),
                    Mvp = table.Column<int>(type: "int", nullable: false),
                    TeamGames = table.Column<int>(type: "int", nullable: false),
                    MainCount = table.Column<int>(type: "int", nullable: false),
                    Main = table.Column<int>(type: "int", nullable: false),
                    Consistency = table.Column<double>(type: "double", nullable: false),
                    Confidence = table.Column<double>(type: "double", nullable: false),
                    IsUploader = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    PlayerId = table.Column<int>(type: "int", nullable: false),
                    Pos = table.Column<int>(type: "int", nullable: false),
                    ArcadeDefeatsSinceLastUpload = table.Column<int>(type: "int", nullable: false)
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
                name: "ComboReplayRatings",
                columns: table => new
                {
                    ComboReplayRatingId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    RatingType = table.Column<int>(type: "int", nullable: false),
                    LeaverType = table.Column<int>(type: "int", nullable: false),
                    ExpectationToWin = table.Column<double>(type: "double", precision: 5, scale: 2, nullable: false),
                    ReplayId = table.Column<int>(type: "int", nullable: false),
                    IsPreRating = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComboReplayRatings", x => x.ComboReplayRatingId);
                    table.ForeignKey(
                        name: "FK_ComboReplayRatings_Replays_ReplayId",
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
                    Name = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Clan = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    GamePos = table.Column<int>(type: "int", nullable: false),
                    Team = table.Column<int>(type: "int", nullable: false),
                    PlayerResult = table.Column<int>(type: "int", nullable: false),
                    Duration = table.Column<int>(type: "int", nullable: false),
                    Race = table.Column<int>(type: "int", nullable: false),
                    OppRace = table.Column<int>(type: "int", nullable: false),
                    APM = table.Column<int>(type: "int", nullable: false),
                    Income = table.Column<int>(type: "int", nullable: false),
                    Army = table.Column<int>(type: "int", nullable: false),
                    Kills = table.Column<int>(type: "int", nullable: false),
                    UpgradesSpent = table.Column<int>(type: "int", nullable: false),
                    IsUploader = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsLeaver = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DidNotUpload = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    TierUpgrades = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Refineries = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LastSpawnHash = table.Column<string>(type: "char(64)", fixedLength: true, maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Downloads = table.Column<int>(type: "int", nullable: false),
                    Views = table.Column<int>(type: "int", nullable: false),
                    ReplayId = table.Column<int>(type: "int", nullable: false),
                    PlayerId = table.Column<int>(type: "int", nullable: false),
                    UpgradeId = table.Column<int>(type: "int", nullable: true)
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
                    table.ForeignKey(
                        name: "FK_ReplayPlayers_Upgrades_UpgradeId",
                        column: x => x.UpgradeId,
                        principalTable: "Upgrades",
                        principalColumn: "UpgradeId");
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
                    ExpectationToWin = table.Column<float>(type: "float", nullable: false),
                    ReplayId = table.Column<int>(type: "int", nullable: false),
                    IsPreRating = table.Column<bool>(type: "tinyint(1)", nullable: false)
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
                name: "UploaderReplays",
                columns: table => new
                {
                    ReplaysReplayId = table.Column<int>(type: "int", nullable: false),
                    UploadersUploaderId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UploaderReplays", x => new { x.ReplaysReplayId, x.UploadersUploaderId });
                    table.ForeignKey(
                        name: "FK_UploaderReplays_Replays_ReplaysReplayId",
                        column: x => x.ReplaysReplayId,
                        principalTable: "Replays",
                        principalColumn: "ReplayId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UploaderReplays_Uploaders_UploadersUploaderId",
                        column: x => x.UploadersUploaderId,
                        principalTable: "Uploaders",
                        principalColumn: "UploaderId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PlayerRatingChanges",
                columns: table => new
                {
                    PlayerRatingChangeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Change24h = table.Column<float>(type: "float", nullable: false),
                    Change10d = table.Column<float>(type: "float", nullable: false),
                    Change30d = table.Column<float>(type: "float", nullable: false),
                    PlayerRatingId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerRatingChanges", x => x.PlayerRatingChangeId);
                    table.ForeignKey(
                        name: "FK_PlayerRatingChanges_PlayerRatings_PlayerRatingId",
                        column: x => x.PlayerRatingId,
                        principalTable: "PlayerRatings",
                        principalColumn: "PlayerRatingId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ComboReplayPlayerRatings",
                columns: table => new
                {
                    ComboReplayPlayerRatingId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    GamePos = table.Column<int>(type: "int", nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    Change = table.Column<double>(type: "double", precision: 5, scale: 2, nullable: false),
                    Games = table.Column<int>(type: "int", nullable: false),
                    Consistency = table.Column<double>(type: "double", precision: 5, scale: 2, nullable: false),
                    Confidence = table.Column<double>(type: "double", precision: 5, scale: 2, nullable: false),
                    ReplayPlayerId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComboReplayPlayerRatings", x => x.ComboReplayPlayerRatingId);
                    table.ForeignKey(
                        name: "FK_ComboReplayPlayerRatings_ReplayPlayers_ReplayPlayerId",
                        column: x => x.ReplayPlayerId,
                        principalTable: "ReplayPlayers",
                        principalColumn: "ReplayPlayerId",
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
                    ReplayPlayerId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerUpgrades", x => x.PlayerUpgradeId);
                    table.ForeignKey(
                        name: "FK_PlayerUpgrades_ReplayPlayers_ReplayPlayerId",
                        column: x => x.ReplayPlayerId,
                        principalTable: "ReplayPlayers",
                        principalColumn: "ReplayPlayerId",
                        onDelete: ReferentialAction.Cascade);
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
                    Gameloop = table.Column<int>(type: "int", nullable: false),
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
                name: "RepPlayerRatings",
                columns: table => new
                {
                    RepPlayerRatingId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    GamePos = table.Column<int>(type: "int", nullable: false),
                    Rating = table.Column<float>(type: "float", nullable: false),
                    RatingChange = table.Column<float>(type: "float", nullable: false),
                    Games = table.Column<int>(type: "int", nullable: false),
                    Consistency = table.Column<float>(type: "float", nullable: false),
                    Confidence = table.Column<float>(type: "float", nullable: false),
                    ReplayPlayerId = table.Column<int>(type: "int", nullable: false),
                    ReplayRatingInfoId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepPlayerRatings", x => x.RepPlayerRatingId);
                    table.ForeignKey(
                        name: "FK_RepPlayerRatings_ReplayPlayers_ReplayPlayerId",
                        column: x => x.ReplayPlayerId,
                        principalTable: "ReplayPlayers",
                        principalColumn: "ReplayPlayerId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RepPlayerRatings_ReplayRatings_ReplayRatingInfoId",
                        column: x => x.ReplayRatingInfoId,
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
                    Count = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    Poss = table.Column<string>(type: "varchar(4000)", maxLength: 4000, nullable: false)
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
                name: "IX_ArcadePlayerRatingChanges_ArcadePlayerRatingId",
                table: "ArcadePlayerRatingChanges",
                column: "ArcadePlayerRatingId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ArcadePlayerRatings_ArcadePlayerId",
                table: "ArcadePlayerRatings",
                column: "ArcadePlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_ArcadePlayerRatings_RatingType",
                table: "ArcadePlayerRatings",
                column: "RatingType");

            migrationBuilder.CreateIndex(
                name: "IX_ArcadePlayers_Name",
                table: "ArcadePlayers",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_ArcadePlayers_RegionId_RealmId_ProfileId",
                table: "ArcadePlayers",
                columns: new[] { "RegionId", "RealmId", "ProfileId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ArcadeReplayPlayerRatings_ArcadeReplayPlayerId",
                table: "ArcadeReplayPlayerRatings",
                column: "ArcadeReplayPlayerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ArcadeReplayPlayerRatings_ArcadeReplayRatingId",
                table: "ArcadeReplayPlayerRatings",
                column: "ArcadeReplayRatingId");

            migrationBuilder.CreateIndex(
                name: "IX_ArcadeReplayPlayers_ArcadePlayerId",
                table: "ArcadeReplayPlayers",
                column: "ArcadePlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_ArcadeReplayPlayers_ArcadeReplayId",
                table: "ArcadeReplayPlayers",
                column: "ArcadeReplayId");

            migrationBuilder.CreateIndex(
                name: "IX_ArcadeReplayRatings_ArcadeReplayId",
                table: "ArcadeReplayRatings",
                column: "ArcadeReplayId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ArcadeReplays_GameMode_CreatedAt",
                table: "ArcadeReplays",
                columns: new[] { "GameMode", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ArcadeReplays_RegionId_BnetBucketId_BnetRecordId",
                table: "ArcadeReplays",
                columns: new[] { "RegionId", "BnetBucketId", "BnetRecordId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ArcadeReplays_RegionId_GameMode_CreatedAt",
                table: "ArcadeReplays",
                columns: new[] { "RegionId", "GameMode", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ArcadeReplays_ReplayHash",
                table: "ArcadeReplays",
                column: "ReplayHash");

            migrationBuilder.CreateIndex(
                name: "IX_BattleNetInfos_UploaderId",
                table: "BattleNetInfos",
                column: "UploaderId");

            migrationBuilder.CreateIndex(
                name: "IX_ComboPlayerRatings_PlayerId",
                table: "ComboPlayerRatings",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_ComboPlayerRatings_RatingType",
                table: "ComboPlayerRatings",
                column: "RatingType");

            migrationBuilder.CreateIndex(
                name: "IX_ComboReplayPlayerRatings_ReplayPlayerId",
                table: "ComboReplayPlayerRatings",
                column: "ReplayPlayerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ComboReplayRatings_RatingType",
                table: "ComboReplayRatings",
                column: "RatingType");

            migrationBuilder.CreateIndex(
                name: "IX_ComboReplayRatings_ReplayId",
                table: "ComboReplayRatings",
                column: "ReplayId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommanderMmrs_Race_OppRace",
                table: "CommanderMmrs",
                columns: new[] { "Race", "OppRace" });

            migrationBuilder.CreateIndex(
                name: "IX_DsUpdates_Time",
                table: "DsUpdates",
                column: "Time");

            migrationBuilder.CreateIndex(
                name: "IX_Events_Name",
                table: "Events",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NoUploadResults_PlayerId",
                table: "NoUploadResults",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerRatingChanges_PlayerRatingId",
                table: "PlayerRatingChanges",
                column: "PlayerRatingId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlayerRatings_PlayerId",
                table: "PlayerRatings",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerRatings_RatingType",
                table: "PlayerRatings",
                column: "RatingType");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerUpgrades_ReplayPlayerId",
                table: "PlayerUpgrades",
                column: "ReplayPlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerUpgrades_UpgradeId",
                table: "PlayerUpgrades",
                column: "UpgradeId");

            migrationBuilder.CreateIndex(
                name: "IX_Players_RegionId_RealmId_ToonId",
                table: "Players",
                columns: new[] { "RegionId", "RealmId", "ToonId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Players_UploaderId",
                table: "Players",
                column: "UploaderId");

            migrationBuilder.CreateIndex(
                name: "IX_RepPlayerRatings_ReplayPlayerId",
                table: "RepPlayerRatings",
                column: "ReplayPlayerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RepPlayerRatings_ReplayRatingInfoId",
                table: "RepPlayerRatings",
                column: "ReplayRatingInfoId");

            migrationBuilder.CreateIndex(
                name: "IX_ReplayEvents_EventId",
                table: "ReplayEvents",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_ReplayPlayers_IsUploader_Team",
                table: "ReplayPlayers",
                columns: new[] { "IsUploader", "Team" });

            migrationBuilder.CreateIndex(
                name: "IX_ReplayPlayers_Kills",
                table: "ReplayPlayers",
                column: "Kills");

            migrationBuilder.CreateIndex(
                name: "IX_ReplayPlayers_LastSpawnHash",
                table: "ReplayPlayers",
                column: "LastSpawnHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReplayPlayers_Name",
                table: "ReplayPlayers",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_ReplayPlayers_PlayerId",
                table: "ReplayPlayers",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_ReplayPlayers_Race",
                table: "ReplayPlayers",
                column: "Race");

            migrationBuilder.CreateIndex(
                name: "IX_ReplayPlayers_Race_OppRace",
                table: "ReplayPlayers",
                columns: new[] { "Race", "OppRace" });

            migrationBuilder.CreateIndex(
                name: "IX_ReplayPlayers_ReplayId",
                table: "ReplayPlayers",
                column: "ReplayId");

            migrationBuilder.CreateIndex(
                name: "IX_ReplayPlayers_UpgradeId",
                table: "ReplayPlayers",
                column: "UpgradeId");

            migrationBuilder.CreateIndex(
                name: "IX_ReplayRatings_RatingType",
                table: "ReplayRatings",
                column: "RatingType");

            migrationBuilder.CreateIndex(
                name: "IX_ReplayRatings_ReplayId",
                table: "ReplayRatings",
                column: "ReplayId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Replays_FileName",
                table: "Replays",
                column: "FileName");

            migrationBuilder.CreateIndex(
                name: "IX_Replays_GameTime_GameMode",
                table: "Replays",
                columns: new[] { "GameTime", "GameMode" });

            migrationBuilder.CreateIndex(
                name: "IX_Replays_GameTime_GameMode_DefaultFilter",
                table: "Replays",
                columns: new[] { "GameTime", "GameMode", "DefaultFilter" });

            migrationBuilder.CreateIndex(
                name: "IX_Replays_GameTime_GameMode_Maxleaver",
                table: "Replays",
                columns: new[] { "GameTime", "GameMode", "Maxleaver" });

            migrationBuilder.CreateIndex(
                name: "IX_Replays_GameTime_GameMode_WinnerTeam",
                table: "Replays",
                columns: new[] { "GameTime", "GameMode", "WinnerTeam" });

            migrationBuilder.CreateIndex(
                name: "IX_Replays_Imported",
                table: "Replays",
                column: "Imported");

            migrationBuilder.CreateIndex(
                name: "IX_Replays_Maxkillsum",
                table: "Replays",
                column: "Maxkillsum");

            migrationBuilder.CreateIndex(
                name: "IX_Replays_ReplayEventId",
                table: "Replays",
                column: "ReplayEventId");

            migrationBuilder.CreateIndex(
                name: "IX_Replays_ReplayHash",
                table: "Replays",
                column: "ReplayHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SpawnUnits_SpawnId",
                table: "SpawnUnits",
                column: "SpawnId");

            migrationBuilder.CreateIndex(
                name: "IX_SpawnUnits_UnitId",
                table: "SpawnUnits",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_Spawns_ReplayPlayerId",
                table: "Spawns",
                column: "ReplayPlayerId");

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
                name: "IX_UploaderReplays_UploadersUploaderId",
                table: "UploaderReplays",
                column: "UploadersUploaderId");

            migrationBuilder.CreateIndex(
                name: "IX_Uploaders_AppGuid",
                table: "Uploaders",
                column: "AppGuid",
                unique: true);

            var SetRatingChange = @"CREATE PROCEDURE `SetRatingChange`()
BEGIN
	declare ratingType_counter int unsigned default 1;
	declare timePeriod_counter int unsigned default 1;
	
    declare ratingType_max int unsigned default 5;
	declare timePeriod_max int unsigned default 4;
    
    declare days int default -1;
    declare hlimit int unsigned default 2;

    SET FOREIGN_KEY_CHECKS = 0;
    TRUNCATE PlayerRatingChanges;
    SET FOREIGN_KEY_CHECKS = 1;
    
	WHILE ratingType_counter < ratingType_max DO
		WHILE timePeriod_counter < timePeriod_max DO
        
            IF timePeriod_counter = 1 THEN
				SET days = -1;
                SET hlimit = 0;
			ELSEIF timePeriod_counter = 2 THEN
				SET days = -10;
                SET hlimit = 0;
			ELSE
				SET days = -30;
                SET hlimit = 0;
			END IF;
            
			CREATE TABLE IF NOT EXISTS TEMP_changetable AS (
				SELECT `p`.`PlayerId`, `pr`.`PlayerRatingId`, ROUND(COALESCE(SUM(`r1`.`RatingChange`), 0.0), 2) AS `RatingChange`
				  FROM `Replays` AS `r`
				  LEFT JOIN `ReplayRatings` AS `r0` ON `r`.`ReplayId` = `r0`.`ReplayId`
				  INNER JOIN `RepPlayerRatings` AS `r1` ON `r0`.`ReplayRatingId` = `r1`.`ReplayRatingInfoId`
				  INNER JOIN `ReplayPlayers` AS `r2` ON `r1`.`ReplayPlayerId` = `r2`.`ReplayPlayerId`
				  INNER JOIN `Players` AS `p` ON `r2`.`PlayerId` = `p`.`PlayerId`
                  INNER JOIN `PlayerRatings` AS `pr` ON `pr`.`PlayerId` = `p`.`PlayerId`
				  WHERE ((`r`.`GameTime` > DATE_ADD(now(), INTERVAL CAST(days * 24 AS signed) hour)) AND `p`.`UploaderId` IS NOT NULL) AND (`r0`.`RatingType` = ratingType_counter) AND (`pr`.`RatingType` = ratingType_counter)
				  GROUP BY `p`.`PlayerId`, `pr`.`PlayerRatingId`
				  HAVING COUNT(*) > hlimit
			  );
			  
			IF timePeriod_counter = 1 THEN
              INSERT INTO PlayerRatingChanges (Change24h, Change10d, Change30d, PlayerRatingId)
				SELECT ROUND(RatingChange, 2), 0, 0, PlayerRatingId
				FROM TEMP_changetable
                ON DUPLICATE KEY UPDATE Change24h =
                (
					SELECT ROUND(RatingChange, 2)
					FROM TEMP_changetable
                    WHERE PlayerRatingChanges.PlayerRatingId = TEMP_changetable.PlayerRatingId
				)
			  ;
			ELSEIF timePeriod_counter = 2 THEN
              INSERT INTO PlayerRatingChanges (Change24h, Change10d, Change30d, PlayerRatingId)
				SELECT 0, ROUND(RatingChange, 2), 0, PlayerRatingId
				FROM TEMP_changetable
                ON DUPLICATE KEY UPDATE Change10d =
                (
					SELECT ROUND(RatingChange, 2)
					FROM TEMP_changetable
                    WHERE PlayerRatingChanges.PlayerRatingId = TEMP_changetable.PlayerRatingId
				)
			  ;
			ELSE
              INSERT INTO PlayerRatingChanges (Change24h, Change10d, Change30d, PlayerRatingId)
				SELECT 0, 0, ROUND(RatingChange, 2), PlayerRatingId
				FROM TEMP_changetable
                ON DUPLICATE KEY UPDATE Change30d =
                (
					SELECT ROUND(RatingChange, 2)
					FROM TEMP_changetable
                    WHERE PlayerRatingChanges.PlayerRatingId = TEMP_changetable.PlayerRatingId
				)
			  ;
			END IF;
             
		  DROP TABLE IF EXISTS TEMP_changetable;
            
          set timePeriod_counter = timePeriod_counter+1;
        END WHILE;
		set ratingType_counter=ratingType_counter+1;
        set timePeriod_counter = 1;
    END WHILE;
END
";
            var SetPlayerRatingPos = @"CREATE PROCEDURE `SetPlayerRatingPos`()
BEGIN
	SET @pos = 0;
    UPDATE PlayerRatings
    SET Pos = (@pos:=@pos+1)
    WHERE RatingType = 1
    ORDER BY Rating DESC, PlayerId;
    
    SET @pos = 0;
    UPDATE PlayerRatings
    SET Pos = (@pos:=@pos+1)
    WHERE RatingType = 2
    ORDER BY Rating DESC, PlayerId;

    SET @pos = 0;
    UPDATE PlayerRatings
    SET Pos = (@pos:=@pos+1)
    WHERE RatingType = 3
    ORDER BY Rating DESC, PlayerId;

    SET @pos = 0;
    UPDATE PlayerRatings
    SET Pos = (@pos:=@pos+1)
    WHERE RatingType = 4
    ORDER BY Rating DESC, PlayerId;
END
";
            var SetComboPlayerRatingPos = @"CREATE PROCEDURE `SetComboPlayerRatingPos`()
BEGIN
	SET @pos = 0;
    UPDATE ComboPlayerRatings
    SET Pos = (@pos:=@pos+1)
    WHERE RatingType = 1
    ORDER BY Rating DESC, PlayerId;
    
    SET @pos = 0;
    UPDATE ComboPlayerRatings
    SET Pos = (@pos:=@pos+1)
    WHERE RatingType = 2
    ORDER BY Rating DESC, PlayerId;

    SET @pos = 0;
    UPDATE ComboPlayerRatings
    SET Pos = (@pos:=@pos+1)
    WHERE RatingType = 3
    ORDER BY Rating DESC, PlayerId;

    SET @pos = 0;
    UPDATE ComboPlayerRatings
    SET Pos = (@pos:=@pos+1)
    WHERE RatingType = 4
    ORDER BY Rating DESC, PlayerId;
END
";
            var SetArcadeRaingChange = @"CREATE PROCEDURE `SetArcadeRatingChange`()
BEGIN
	declare ratingType_counter int unsigned default 1;
	declare timePeriod_counter int unsigned default 1;
	
    declare ratingType_max int unsigned default 3;
	declare timePeriod_max int unsigned default 4;
    
    declare days int default -1;
    declare hlimit int unsigned default 2;

    SET FOREIGN_KEY_CHECKS = 0;
    TRUNCATE ArcadePlayerRatingChanges;
    SET FOREIGN_KEY_CHECKS = 1;
    
	WHILE ratingType_counter < ratingType_max DO
		WHILE timePeriod_counter < timePeriod_max DO
        
            IF timePeriod_counter = 1 THEN
				SET days = -1;
                SET hlimit = 5;
			ELSEIF timePeriod_counter = 2 THEN
				SET days = -10;
                SET hlimit = 10;
			ELSE
				SET days = -30;
                SET hlimit = 20;
			END IF;
            
			CREATE TABLE IF NOT EXISTS TEMP_changetable AS (
				SELECT `p`.`ArcadePlayerId`, `pr`.`ArcadePlayerRatingId`, ROUND(COALESCE(SUM(`r1`.`RatingChange`), 0.0), 2) AS `RatingChange`
				  FROM `ArcadeReplays` AS `r`
				  LEFT JOIN `ArcadeReplayRatings` AS `r0` ON `r`.`ArcadeReplayId` = `r0`.`ArcadeReplayId`
				  INNER JOIN `ArcadeReplayPlayerRatings` AS `r1` ON `r0`.`ArcadeReplayRatingId` = `r1`.`ArcadeReplayRatingId`
				  INNER JOIN `ArcadeReplayPlayers` AS `r2` ON `r1`.`ArcadeReplayPlayerId` = `r2`.`ArcadeReplayPlayerId`
				  INNER JOIN `ArcadePlayers` AS `p` ON `r2`.`ArcadePlayerId` = `p`.`ArcadePlayerId`
                  INNER JOIN `ArcadePlayerRatings` AS `pr` ON `pr`.`ArcadePlayerId` = `p`.`ArcadePlayerId`
				  WHERE ((`r`.`CreatedAt` > DATE_ADD(now(), INTERVAL CAST(days * 24 AS signed) hour)) AND (`r0`.`RatingType` = ratingType_counter) AND (`pr`.`RatingType` = ratingType_counter) AND (`pr`.`Games` > 20))
				  GROUP BY `p`.`ArcadePlayerId`, `pr`.`ArcadePlayerRatingId`
				  HAVING COUNT(*) > hlimit
			  );
			  
			IF timePeriod_counter = 1 THEN
              INSERT INTO ArcadePlayerRatingChanges (Change24h, Change10d, Change30d, ArcadePlayerRatingId)
				SELECT ROUND(RatingChange, 2), 0, 0, ArcadePlayerRatingId
				FROM TEMP_changetable
                ON DUPLICATE KEY UPDATE Change24h =
                (
					SELECT ROUND(RatingChange, 2)
					FROM TEMP_changetable
                    WHERE ArcadePlayerRatingChanges.ArcadePlayerRatingId = TEMP_changetable.ArcadePlayerRatingId
				)
			  ;
			ELSEIF timePeriod_counter = 2 THEN
              INSERT INTO ArcadePlayerRatingChanges (Change24h, Change10d, Change30d, ArcadePlayerRatingId)
				SELECT 0, ROUND(RatingChange, 2), 0, ArcadePlayerRatingId
				FROM TEMP_changetable
                ON DUPLICATE KEY UPDATE Change10d =
                (
					SELECT ROUND(RatingChange, 2)
					FROM TEMP_changetable
                    WHERE ArcadePlayerRatingChanges.ArcadePlayerRatingId = TEMP_changetable.ArcadePlayerRatingId
				)
			  ;
			ELSE
              INSERT INTO ArcadePlayerRatingChanges (Change24h, Change10d, Change30d, ArcadePlayerRatingId)
				SELECT 0, 0, ROUND(RatingChange, 2), ArcadePlayerRatingId
				FROM TEMP_changetable
                ON DUPLICATE KEY UPDATE Change30d =
                (
					SELECT ROUND(RatingChange, 2)
					FROM TEMP_changetable
                    WHERE ArcadePlayerRatingChanges.ArcadePlayerRatingId = TEMP_changetable.ArcadePlayerRatingId
				)
			  ;
			END IF;
             
		  DROP TABLE IF EXISTS TEMP_changetable;
            
          set timePeriod_counter = timePeriod_counter+1;
        END WHILE;
		set ratingType_counter=ratingType_counter+1;
        set timePeriod_counter = 1;
    END WHILE;
END
";

            var SetArcadePlayerRatingPos = @"CREATE PROCEDURE `SetArcadePlayerRatingPos`()
BEGIN
	SET @pos = 0;
    UPDATE ArcadePlayerRatings
    SET Pos = (@pos:=@pos+1)
    WHERE RatingType = 1
    ORDER BY Rating DESC, ArcadePlayerId;
    
    SET @pos = 0;
    UPDATE ArcadePlayerRatings
    SET Pos = (@pos:=@pos+1)
    WHERE RatingType = 2
    ORDER BY Rating DESC, ArcadePlayerId;

    SET @pos = 0;
    UPDATE ArcadePlayerRatings
    SET Pos = (@pos:=@pos+1)
    WHERE RatingType = 3
    ORDER BY Rating DESC, ArcadePlayerId;

    SET @pos = 0;
    UPDATE ArcadePlayerRatings
    SET Pos = (@pos:=@pos+1)
    WHERE RatingType = 4
    ORDER BY Rating DESC, ArcadePlayerId;
END
";
            migrationBuilder.Sql(SetArcadePlayerRatingPos);
            migrationBuilder.Sql(SetArcadeRaingChange);
            migrationBuilder.Sql(SetComboPlayerRatingPos);
            migrationBuilder.Sql(SetPlayerRatingPos);
            migrationBuilder.Sql(SetRatingChange);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ArcadePlayerRatingChanges");

            migrationBuilder.DropTable(
                name: "ArcadeReplayPlayerRatings");

            migrationBuilder.DropTable(
                name: "BattleNetInfos");

            migrationBuilder.DropTable(
                name: "ComboPlayerRatings");

            migrationBuilder.DropTable(
                name: "ComboReplayPlayerRatings");

            migrationBuilder.DropTable(
                name: "ComboReplayRatings");

            migrationBuilder.DropTable(
                name: "CommanderMmrs");

            migrationBuilder.DropTable(
                name: "DsUpdates");

            migrationBuilder.DropTable(
                name: "FunStatMemories");

            migrationBuilder.DropTable(
                name: "NoUploadResults");

            migrationBuilder.DropTable(
                name: "PlayerRatingChanges");

            migrationBuilder.DropTable(
                name: "PlayerUpgrades");

            migrationBuilder.DropTable(
                name: "RepPlayerRatings");

            migrationBuilder.DropTable(
                name: "ReplayDownloadCounts");

            migrationBuilder.DropTable(
                name: "ReplayViewCounts");

            migrationBuilder.DropTable(
                name: "SkipReplays");

            migrationBuilder.DropTable(
                name: "SpawnUnits");

            migrationBuilder.DropTable(
                name: "UploaderReplays");

            migrationBuilder.DropTable(
                name: "ArcadePlayerRatings");

            migrationBuilder.DropTable(
                name: "ArcadeReplayPlayers");

            migrationBuilder.DropTable(
                name: "ArcadeReplayRatings");

            migrationBuilder.DropTable(
                name: "PlayerRatings");

            migrationBuilder.DropTable(
                name: "ReplayRatings");

            migrationBuilder.DropTable(
                name: "Spawns");

            migrationBuilder.DropTable(
                name: "Units");

            migrationBuilder.DropTable(
                name: "ArcadePlayers");

            migrationBuilder.DropTable(
                name: "ArcadeReplays");

            migrationBuilder.DropTable(
                name: "ReplayPlayers");

            migrationBuilder.DropTable(
                name: "Players");

            migrationBuilder.DropTable(
                name: "Replays");

            migrationBuilder.DropTable(
                name: "Upgrades");

            migrationBuilder.DropTable(
                name: "Uploaders");

            migrationBuilder.DropTable(
                name: "ReplayEvents");

            migrationBuilder.DropTable(
                name: "Events");
        }
    }
}
