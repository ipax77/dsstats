using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MysqlMigrations.Migrations
{
    public partial class Init : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
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
                name: "Events",
                columns: table => new
                {
                    EventId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EventGuid = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    EventStart = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Events", x => x.EventId);
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
                    Mmr = table.Column<double>(type: "double", nullable: false),
                    MmrStd = table.Column<double>(type: "double", nullable: false),
                    MmrOverTime = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MmrStdOverTime = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    GamesCmdr = table.Column<int>(type: "int", nullable: false),
                    WinsCmdr = table.Column<int>(type: "int", nullable: false),
                    MvpCmdr = table.Column<int>(type: "int", nullable: false),
                    TeamGamesCmdr = table.Column<int>(type: "int", nullable: false),
                    GamesStd = table.Column<int>(type: "int", nullable: false),
                    WinsStd = table.Column<int>(type: "int", nullable: false),
                    MvpStd = table.Column<int>(type: "int", nullable: false),
                    TeamGamesStd = table.Column<int>(type: "int", nullable: false),
                    MainCommander = table.Column<int>(type: "int", nullable: false),
                    MainCount = table.Column<int>(type: "int", nullable: false),
                    NotUploadCount = table.Column<int>(type: "int", nullable: false),
                    LeaverCount = table.Column<int>(type: "int", nullable: false),
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
                name: "Replays",
                columns: table => new
                {
                    ReplayId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    FileName = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TournamentEdition = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    GameTime = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: false),
                    Duration = table.Column<int>(type: "int", nullable: false),
                    WinnerTeam = table.Column<int>(type: "int", nullable: false),
                    PlayerResult = table.Column<int>(type: "int", nullable: false),
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
                    MmrChange = table.Column<float>(type: "float", nullable: true),
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
                name: "IX_BattleNetInfos_UploaderId",
                table: "BattleNetInfos",
                column: "UploaderId");

            migrationBuilder.CreateIndex(
                name: "IX_CommanderMmrs_Race_OppRace",
                table: "CommanderMmrs",
                columns: new[] { "Race", "OppRace" });

            migrationBuilder.CreateIndex(
                name: "IX_Events_Name",
                table: "Events",
                column: "Name",
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
                name: "IX_Players_ToonId",
                table: "Players",
                column: "ToonId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Players_UploaderId",
                table: "Players",
                column: "UploaderId");

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
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BattleNetInfos");

            migrationBuilder.DropTable(
                name: "CommanderMmrs");

            migrationBuilder.DropTable(
                name: "PlayerUpgrades");

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
                name: "Spawns");

            migrationBuilder.DropTable(
                name: "Units");

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
