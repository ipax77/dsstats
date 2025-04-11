using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MySqlMigrations.Migrations
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
                    GlobalRating = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Players", x => x.PlayerId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Replays",
                columns: table => new
                {
                    ReplayId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    GameTime = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: false),
                    Imported = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: true),
                    GameMode = table.Column<int>(type: "int", nullable: false),
                    IsTE = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ReplayHash = table.Column<string>(type: "char(64)", fixedLength: true, maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Region = table.Column<int>(type: "int", nullable: false),
                    Duration = table.Column<int>(type: "int", nullable: false),
                    PlayerCount = table.Column<int>(type: "int", nullable: false),
                    WinnerTeam = table.Column<int>(type: "int", nullable: false),
                    Bunker = table.Column<int>(type: "int", nullable: false),
                    Cannon = table.Column<int>(type: "int", nullable: false),
                    Minkillsum = table.Column<int>(type: "int", nullable: false),
                    Maxkillsum = table.Column<int>(type: "int", nullable: false),
                    Minarmy = table.Column<int>(type: "int", nullable: false),
                    Minincome = table.Column<int>(type: "int", nullable: false),
                    Maxleaver = table.Column<int>(type: "int", nullable: false),
                    Views = table.Column<int>(type: "int", nullable: false),
                    MiddleBinary = table.Column<byte[]>(type: "varbinary(8000)", nullable: false),
                    CommandersTeam1 = table.Column<string>(type: "varchar(15)", maxLength: 15, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CommandersTeam2 = table.Column<string>(type: "varchar(15)", maxLength: 15, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Replays", x => x.ReplayId);
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
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Upgrades", x => x.UpgradeId);
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
                    Wins = table.Column<int>(type: "int", nullable: false),
                    Mvp = table.Column<int>(type: "int", nullable: false),
                    Rating = table.Column<double>(type: "double", nullable: false),
                    Consistency = table.Column<double>(type: "double", nullable: false),
                    Confidence = table.Column<double>(type: "double", nullable: false),
                    Pos = table.Column<int>(type: "int", nullable: false),
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
                    OpponentId = table.Column<int>(type: "int", nullable: true),
                    APM = table.Column<int>(type: "int", nullable: false),
                    Income = table.Column<int>(type: "int", nullable: false),
                    Army = table.Column<int>(type: "int", nullable: false),
                    Kills = table.Column<int>(type: "int", nullable: false),
                    UpgradesSpent = table.Column<int>(type: "int", nullable: false),
                    Refineries = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TierUpgrades = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LastSpawnHash = table.Column<string>(type: "char(64)", fixedLength: true, maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsUploader = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    PlayerId = table.Column<int>(type: "int", nullable: false),
                    ReplayId = table.Column<int>(type: "int", nullable: false)
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
                        name: "FK_ReplayPlayers_ReplayPlayers_OpponentId",
                        column: x => x.OpponentId,
                        principalTable: "ReplayPlayers",
                        principalColumn: "ReplayPlayerId");
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
                    ExpectationToWin = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
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
                name: "ReplayPlayerRatings",
                columns: table => new
                {
                    ReplayPlayerRatingId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    RatingType = table.Column<int>(type: "int", nullable: false),
                    GamePos = table.Column<int>(type: "int", nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    Change = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    Games = table.Column<int>(type: "int", nullable: false),
                    Consistency = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    Confidence = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    ReplayPlayerId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReplayPlayerRatings", x => x.ReplayPlayerRatingId);
                    table.ForeignKey(
                        name: "FK_ReplayPlayerRatings_ReplayPlayers_ReplayPlayerId",
                        column: x => x.ReplayPlayerId,
                        principalTable: "ReplayPlayers",
                        principalColumn: "ReplayPlayerId",
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
                    Count = table.Column<int>(type: "int", nullable: false),
                    PositionsBinary = table.Column<byte[]>(type: "varbinary(8000)", nullable: false),
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
                name: "IX_PlayerRatings_PlayerId_RatingType",
                table: "PlayerRatings",
                columns: new[] { "PlayerId", "RatingType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Players_RegionId_RealmId_ToonId",
                table: "Players",
                columns: new[] { "RegionId", "RealmId", "ToonId" },
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
                name: "IX_ReplayPlayerRatings_RatingType_ReplayPlayerId",
                table: "ReplayPlayerRatings",
                columns: new[] { "RatingType", "ReplayPlayerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReplayPlayerRatings_ReplayPlayerId",
                table: "ReplayPlayerRatings",
                column: "ReplayPlayerId");

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
                name: "IX_ReplayPlayers_OpponentId",
                table: "ReplayPlayers",
                column: "OpponentId");

            migrationBuilder.CreateIndex(
                name: "IX_ReplayPlayers_PlayerId",
                table: "ReplayPlayers",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_ReplayPlayers_Race",
                table: "ReplayPlayers",
                column: "Race");

            migrationBuilder.CreateIndex(
                name: "IX_ReplayPlayers_ReplayId",
                table: "ReplayPlayers",
                column: "ReplayId");

            migrationBuilder.CreateIndex(
                name: "IX_ReplayRatings_RatingType_ReplayId",
                table: "ReplayRatings",
                columns: new[] { "RatingType", "ReplayId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReplayRatings_ReplayId",
                table: "ReplayRatings",
                column: "ReplayId");

            migrationBuilder.CreateIndex(
                name: "IX_Replays_GameTime",
                table: "Replays",
                column: "GameTime");

            migrationBuilder.CreateIndex(
                name: "IX_Replays_GameTime_GameMode",
                table: "Replays",
                columns: new[] { "GameTime", "GameMode" });

            migrationBuilder.CreateIndex(
                name: "IX_Replays_Imported",
                table: "Replays",
                column: "Imported");

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
                name: "IX_Units_Name",
                table: "Units",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Upgrades_Name",
                table: "Upgrades",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlayerRatings");

            migrationBuilder.DropTable(
                name: "PlayerUpgrades");

            migrationBuilder.DropTable(
                name: "ReplayPlayerRatings");

            migrationBuilder.DropTable(
                name: "ReplayRatings");

            migrationBuilder.DropTable(
                name: "SpawnUnits");

            migrationBuilder.DropTable(
                name: "Upgrades");

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
        }
    }
}
