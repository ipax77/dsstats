using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SqliteMigrations.Migrations
{
    public partial class Init : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CommanderMmrs",
                columns: table => new
                {
                    CommanderMmrId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Race = table.Column<int>(type: "INTEGER", nullable: false),
                    OppRace = table.Column<int>(type: "INTEGER", nullable: false),
                    SynergyMmr = table.Column<double>(type: "REAL", nullable: false),
                    AntiSynergyMmr = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommanderMmrs", x => x.CommanderMmrId);
                });

            migrationBuilder.CreateTable(
                name: "Events",
                columns: table => new
                {
                    EventId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    EventGuid = table.Column<Guid>(type: "TEXT", nullable: false),
                    EventStart = table.Column<DateTime>(type: "TEXT", precision: 0, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Events", x => x.EventId);
                });

            migrationBuilder.CreateTable(
                name: "ReplayDownloadCounts",
                columns: table => new
                {
                    ReplayDownloadCountId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ReplayHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReplayDownloadCounts", x => x.ReplayDownloadCountId);
                });

            migrationBuilder.CreateTable(
                name: "ReplayViewCounts",
                columns: table => new
                {
                    ReplayViewCountId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ReplayHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReplayViewCounts", x => x.ReplayViewCountId);
                });

            migrationBuilder.CreateTable(
                name: "SkipReplays",
                columns: table => new
                {
                    SkipReplayId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Path = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkipReplays", x => x.SkipReplayId);
                });

            migrationBuilder.CreateTable(
                name: "Units",
                columns: table => new
                {
                    UnitId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Units", x => x.UnitId);
                });

            migrationBuilder.CreateTable(
                name: "Upgrades",
                columns: table => new
                {
                    UpgradeId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Cost = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Upgrades", x => x.UpgradeId);
                });

            migrationBuilder.CreateTable(
                name: "Uploaders",
                columns: table => new
                {
                    UploaderId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AppGuid = table.Column<Guid>(type: "TEXT", nullable: false),
                    AppVersion = table.Column<string>(type: "TEXT", nullable: false),
                    Identifier = table.Column<string>(type: "TEXT", nullable: false),
                    LatestUpload = table.Column<DateTime>(type: "TEXT", precision: 0, nullable: false),
                    LatestReplay = table.Column<DateTime>(type: "TEXT", precision: 0, nullable: false),
                    Games = table.Column<int>(type: "INTEGER", nullable: false),
                    Wins = table.Column<int>(type: "INTEGER", nullable: false),
                    Mvp = table.Column<int>(type: "INTEGER", nullable: false),
                    MainCommander = table.Column<int>(type: "INTEGER", nullable: false),
                    MainCount = table.Column<int>(type: "INTEGER", nullable: false),
                    TeamGames = table.Column<int>(type: "INTEGER", nullable: false),
                    UploadLastDisabled = table.Column<DateTime>(type: "TEXT", precision: 0, nullable: false),
                    UploadDisabledCount = table.Column<int>(type: "INTEGER", nullable: false),
                    UploadIsDisabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Uploaders", x => x.UploaderId);
                });

            migrationBuilder.CreateTable(
                name: "ReplayEvents",
                columns: table => new
                {
                    ReplayEventId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Round = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    WinnerTeam = table.Column<string>(type: "TEXT", nullable: false),
                    RunnerTeam = table.Column<string>(type: "TEXT", nullable: false),
                    Ban1 = table.Column<int>(type: "INTEGER", nullable: false),
                    Ban2 = table.Column<int>(type: "INTEGER", nullable: false),
                    Ban3 = table.Column<int>(type: "INTEGER", nullable: false),
                    Ban4 = table.Column<int>(type: "INTEGER", nullable: false),
                    Ban5 = table.Column<int>(type: "INTEGER", nullable: false),
                    EventId = table.Column<int>(type: "INTEGER", nullable: false)
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
                });

            migrationBuilder.CreateTable(
                name: "BattleNetInfos",
                columns: table => new
                {
                    BattleNetInfoId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BattleNetId = table.Column<int>(type: "INTEGER", nullable: false),
                    UploaderId = table.Column<int>(type: "INTEGER", nullable: false)
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
                });

            migrationBuilder.CreateTable(
                name: "Players",
                columns: table => new
                {
                    PlayerId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ToonId = table.Column<int>(type: "INTEGER", nullable: false),
                    RegionId = table.Column<int>(type: "INTEGER", nullable: false),
                    Mmr = table.Column<double>(type: "REAL", nullable: false),
                    MmrStd = table.Column<double>(type: "REAL", nullable: false),
                    MmrOverTime = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    MmrStdOverTime = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    GamesCmdr = table.Column<int>(type: "INTEGER", nullable: false),
                    WinsCmdr = table.Column<int>(type: "INTEGER", nullable: false),
                    MvpCmdr = table.Column<int>(type: "INTEGER", nullable: false),
                    TeamGamesCmdr = table.Column<int>(type: "INTEGER", nullable: false),
                    GamesStd = table.Column<int>(type: "INTEGER", nullable: false),
                    WinsStd = table.Column<int>(type: "INTEGER", nullable: false),
                    MvpStd = table.Column<int>(type: "INTEGER", nullable: false),
                    TeamGamesStd = table.Column<int>(type: "INTEGER", nullable: false),
                    MainCommander = table.Column<int>(type: "INTEGER", nullable: false),
                    MainCount = table.Column<int>(type: "INTEGER", nullable: false),
                    NotUploadCount = table.Column<int>(type: "INTEGER", nullable: false),
                    LeaverCount = table.Column<int>(type: "INTEGER", nullable: false),
                    UploaderId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Players", x => x.PlayerId);
                    table.ForeignKey(
                        name: "FK_Players_Uploaders_UploaderId",
                        column: x => x.UploaderId,
                        principalTable: "Uploaders",
                        principalColumn: "UploaderId");
                });

            migrationBuilder.CreateTable(
                name: "Replays",
                columns: table => new
                {
                    ReplayId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FileName = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    TournamentEdition = table.Column<bool>(type: "INTEGER", nullable: false),
                    GameTime = table.Column<DateTime>(type: "TEXT", precision: 0, nullable: false),
                    Duration = table.Column<int>(type: "INTEGER", nullable: false),
                    WinnerTeam = table.Column<int>(type: "INTEGER", nullable: false),
                    PlayerResult = table.Column<int>(type: "INTEGER", nullable: false),
                    GameMode = table.Column<int>(type: "INTEGER", nullable: false),
                    Objective = table.Column<int>(type: "INTEGER", nullable: false),
                    Bunker = table.Column<int>(type: "INTEGER", nullable: false),
                    Cannon = table.Column<int>(type: "INTEGER", nullable: false),
                    Minkillsum = table.Column<int>(type: "INTEGER", nullable: false),
                    Maxkillsum = table.Column<int>(type: "INTEGER", nullable: false),
                    Minarmy = table.Column<int>(type: "INTEGER", nullable: false),
                    Minincome = table.Column<int>(type: "INTEGER", nullable: false),
                    Maxleaver = table.Column<int>(type: "INTEGER", nullable: false),
                    Playercount = table.Column<byte>(type: "INTEGER", nullable: false),
                    ReplayHash = table.Column<string>(type: "TEXT", fixedLength: true, maxLength: 64, nullable: false),
                    DefaultFilter = table.Column<bool>(type: "INTEGER", nullable: false),
                    Views = table.Column<int>(type: "INTEGER", nullable: false),
                    Downloads = table.Column<int>(type: "INTEGER", nullable: false),
                    Middle = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    CommandersTeam1 = table.Column<string>(type: "TEXT", nullable: false),
                    CommandersTeam2 = table.Column<string>(type: "TEXT", nullable: false),
                    ReplayEventId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Replays", x => x.ReplayId);
                    table.ForeignKey(
                        name: "FK_Replays_ReplayEvents_ReplayEventId",
                        column: x => x.ReplayEventId,
                        principalTable: "ReplayEvents",
                        principalColumn: "ReplayEventId");
                });

            migrationBuilder.CreateTable(
                name: "ReplayPlayers",
                columns: table => new
                {
                    ReplayPlayerId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Clan = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    GamePos = table.Column<int>(type: "INTEGER", nullable: false),
                    Team = table.Column<int>(type: "INTEGER", nullable: false),
                    PlayerResult = table.Column<int>(type: "INTEGER", nullable: false),
                    MmrChange = table.Column<float>(type: "REAL", nullable: true),
                    Duration = table.Column<int>(type: "INTEGER", nullable: false),
                    Race = table.Column<int>(type: "INTEGER", nullable: false),
                    OppRace = table.Column<int>(type: "INTEGER", nullable: false),
                    APM = table.Column<int>(type: "INTEGER", nullable: false),
                    Income = table.Column<int>(type: "INTEGER", nullable: false),
                    Army = table.Column<int>(type: "INTEGER", nullable: false),
                    Kills = table.Column<int>(type: "INTEGER", nullable: false),
                    UpgradesSpent = table.Column<int>(type: "INTEGER", nullable: false),
                    IsUploader = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsLeaver = table.Column<bool>(type: "INTEGER", nullable: false),
                    DidNotUpload = table.Column<bool>(type: "INTEGER", nullable: false),
                    TierUpgrades = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    Refineries = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    LastSpawnHash = table.Column<string>(type: "TEXT", fixedLength: true, maxLength: 64, nullable: true),
                    Downloads = table.Column<int>(type: "INTEGER", nullable: false),
                    Views = table.Column<int>(type: "INTEGER", nullable: false),
                    ReplayId = table.Column<int>(type: "INTEGER", nullable: false),
                    PlayerId = table.Column<int>(type: "INTEGER", nullable: false),
                    UpgradeId = table.Column<int>(type: "INTEGER", nullable: true)
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
                });

            migrationBuilder.CreateTable(
                name: "UploaderReplays",
                columns: table => new
                {
                    ReplaysReplayId = table.Column<int>(type: "INTEGER", nullable: false),
                    UploadersUploaderId = table.Column<int>(type: "INTEGER", nullable: false)
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
                });

            migrationBuilder.CreateTable(
                name: "PlayerUpgrades",
                columns: table => new
                {
                    PlayerUpgradeId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Gameloop = table.Column<int>(type: "INTEGER", nullable: false),
                    UpgradeId = table.Column<int>(type: "INTEGER", nullable: false),
                    ReplayPlayerId = table.Column<int>(type: "INTEGER", nullable: false)
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
                });

            migrationBuilder.CreateTable(
                name: "Spawns",
                columns: table => new
                {
                    SpawnId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Gameloop = table.Column<int>(type: "INTEGER", nullable: false),
                    Breakpoint = table.Column<int>(type: "INTEGER", nullable: false),
                    Income = table.Column<int>(type: "INTEGER", nullable: false),
                    GasCount = table.Column<int>(type: "INTEGER", nullable: false),
                    ArmyValue = table.Column<int>(type: "INTEGER", nullable: false),
                    KilledValue = table.Column<int>(type: "INTEGER", nullable: false),
                    UpgradeSpent = table.Column<int>(type: "INTEGER", nullable: false),
                    ReplayPlayerId = table.Column<int>(type: "INTEGER", nullable: false)
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
                });

            migrationBuilder.CreateTable(
                name: "SpawnUnits",
                columns: table => new
                {
                    SpawnUnitId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Count = table.Column<byte>(type: "INTEGER", nullable: false),
                    Poss = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    UnitId = table.Column<int>(type: "INTEGER", nullable: false),
                    SpawnId = table.Column<int>(type: "INTEGER", nullable: false)
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
                });

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
                name: "IX_Players_ToonId",
                table: "Players",
                column: "ToonId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Players_UploaderId",
                table: "Players",
                column: "UploaderId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerUpgrades_ReplayPlayerId",
                table: "PlayerUpgrades",
                column: "ReplayPlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerUpgrades_UpgradeId",
                table: "PlayerUpgrades",
                column: "UpgradeId");

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
