using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SqliteMigrations.Migrations
{
    public partial class Arcade : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "Imported",
                table: "Replays",
                type: "TEXT",
                precision: 0,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ArcadePlayers",
                columns: table => new
                {
                    ArcadePlayerId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    RegionId = table.Column<int>(type: "INTEGER", nullable: false),
                    RealmId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProfileId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArcadePlayers", x => x.ArcadePlayerId);
                });

            migrationBuilder.CreateTable(
                name: "ArcadeReplays",
                columns: table => new
                {
                    ArcadeReplayId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RegionId = table.Column<int>(type: "INTEGER", nullable: false),
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    GameMode = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", precision: 0, nullable: false),
                    Duration = table.Column<int>(type: "INTEGER", nullable: false),
                    PlayerCount = table.Column<int>(type: "INTEGER", nullable: false),
                    TournamentEdition = table.Column<bool>(type: "INTEGER", nullable: false),
                    WinnerTeam = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArcadeReplays", x => x.ArcadeReplayId);
                });

            migrationBuilder.CreateTable(
                name: "ArcadePlayerRatings",
                columns: table => new
                {
                    ArcadePlayerRatingId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RatingType = table.Column<int>(type: "INTEGER", nullable: false),
                    Rating = table.Column<double>(type: "REAL", nullable: false),
                    Pos = table.Column<int>(type: "INTEGER", nullable: false),
                    Games = table.Column<int>(type: "INTEGER", nullable: false),
                    Wins = table.Column<int>(type: "INTEGER", nullable: false),
                    Mvp = table.Column<int>(type: "INTEGER", nullable: false),
                    TeamGames = table.Column<int>(type: "INTEGER", nullable: false),
                    MainCount = table.Column<int>(type: "INTEGER", nullable: false),
                    Main = table.Column<int>(type: "INTEGER", nullable: false),
                    MmrOverTime = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    Consistency = table.Column<double>(type: "REAL", nullable: false),
                    Confidence = table.Column<double>(type: "REAL", nullable: false),
                    IsUploader = table.Column<bool>(type: "INTEGER", nullable: false),
                    ArcadePlayerId = table.Column<int>(type: "INTEGER", nullable: false)
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
                });

            migrationBuilder.CreateTable(
                name: "ArcadeReplayPlayers",
                columns: table => new
                {
                    ArcadeReplayPlayerId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    SlotNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    Team = table.Column<int>(type: "INTEGER", nullable: false),
                    Discriminator = table.Column<int>(type: "INTEGER", nullable: false),
                    PlayerResult = table.Column<int>(type: "INTEGER", nullable: false),
                    ArcadePlayerId = table.Column<int>(type: "INTEGER", nullable: false),
                    ArcadeReplayId = table.Column<int>(type: "INTEGER", nullable: false)
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
                });

            migrationBuilder.CreateTable(
                name: "ArcadeReplayRatings",
                columns: table => new
                {
                    ArcadeReplayRatingId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RatingType = table.Column<int>(type: "INTEGER", nullable: false),
                    LeaverType = table.Column<int>(type: "INTEGER", nullable: false),
                    ExpectationToWin = table.Column<float>(type: "REAL", nullable: false),
                    ArcadeReplayId = table.Column<int>(type: "INTEGER", nullable: false)
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
                name: "ArcadePlayerRatingChanges",
                columns: table => new
                {
                    ArcadePlayerRatingChangeId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Change24h = table.Column<float>(type: "REAL", nullable: false),
                    Change10d = table.Column<float>(type: "REAL", nullable: false),
                    Change30d = table.Column<float>(type: "REAL", nullable: false),
                    ArcadePlayerRatingId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArcadePlayerRatingChanges", x => x.ArcadePlayerRatingChangeId);
                    table.ForeignKey(
                        name: "FK_ArcadePlayerRatingChanges_ArcadePlayerRatings_ArcadePlayerRatingId",
                        column: x => x.ArcadePlayerRatingId,
                        principalTable: "ArcadePlayerRatings",
                        principalColumn: "ArcadePlayerRatingId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ArcadeReplayPlayerRatings",
                columns: table => new
                {
                    ArcadeReplayPlayerRatingId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GamePos = table.Column<int>(type: "INTEGER", nullable: false),
                    Rating = table.Column<float>(type: "REAL", nullable: false),
                    RatingChange = table.Column<float>(type: "REAL", nullable: false),
                    Games = table.Column<int>(type: "INTEGER", nullable: false),
                    Consistency = table.Column<float>(type: "REAL", nullable: false),
                    Confidence = table.Column<float>(type: "REAL", nullable: false),
                    ArcadeReplayPlayerId = table.Column<int>(type: "INTEGER", nullable: false),
                    ArcadeReplayRatingId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArcadeReplayPlayerRatings", x => x.ArcadeReplayPlayerRatingId);
                    table.ForeignKey(
                        name: "FK_ArcadeReplayPlayerRatings_ArcadeReplayPlayers_ArcadeReplayPlayerId",
                        column: x => x.ArcadeReplayPlayerId,
                        principalTable: "ArcadeReplayPlayers",
                        principalColumn: "ArcadeReplayPlayerId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ArcadeReplayPlayerRatings_ArcadeReplayRatings_ArcadeReplayRatingId",
                        column: x => x.ArcadeReplayRatingId,
                        principalTable: "ArcadeReplayRatings",
                        principalColumn: "ArcadeReplayRatingId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Replays_Imported",
                table: "Replays",
                column: "Imported");

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
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ArcadePlayerRatingChanges");

            migrationBuilder.DropTable(
                name: "ArcadeReplayPlayerRatings");

            migrationBuilder.DropTable(
                name: "ArcadePlayerRatings");

            migrationBuilder.DropTable(
                name: "ArcadeReplayPlayers");

            migrationBuilder.DropTable(
                name: "ArcadeReplayRatings");

            migrationBuilder.DropTable(
                name: "ArcadePlayers");

            migrationBuilder.DropTable(
                name: "ArcadeReplays");

            migrationBuilder.DropIndex(
                name: "IX_Replays_Imported",
                table: "Replays");

            migrationBuilder.DropColumn(
                name: "Imported",
                table: "Replays");
        }
    }
}
