using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SqliteMigrations.Migrations
{
    public partial class ComboRatings : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ComboPlayerRatings",
                columns: table => new
                {
                    ComboPlayerRatingId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RatingType = table.Column<int>(type: "INTEGER", nullable: false),
                    Games = table.Column<int>(type: "INTEGER", nullable: false),
                    Wins = table.Column<int>(type: "INTEGER", nullable: false),
                    Rating = table.Column<double>(type: "REAL", nullable: false),
                    Consistency = table.Column<double>(type: "REAL", nullable: false),
                    Confidence = table.Column<double>(type: "REAL", nullable: false),
                    Pos = table.Column<int>(type: "INTEGER", nullable: false),
                    PlayerId = table.Column<int>(type: "INTEGER", nullable: false)
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
                });

            migrationBuilder.CreateTable(
                name: "ComboReplayPlayerRatings",
                columns: table => new
                {
                    ComboReplayPlayerRatingId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GamePos = table.Column<int>(type: "INTEGER", nullable: false),
                    Rating = table.Column<int>(type: "INTEGER", nullable: false),
                    Change = table.Column<double>(type: "REAL", precision: 5, scale: 2, nullable: false),
                    Games = table.Column<int>(type: "INTEGER", nullable: false),
                    Consistency = table.Column<double>(type: "REAL", precision: 5, scale: 2, nullable: false),
                    Confidence = table.Column<double>(type: "REAL", precision: 5, scale: 2, nullable: false),
                    ReplayPlayerId = table.Column<int>(type: "INTEGER", nullable: false)
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
                });

            migrationBuilder.CreateTable(
                name: "ComboReplayRatings",
                columns: table => new
                {
                    ComboReplayRatingId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RatingType = table.Column<int>(type: "INTEGER", nullable: false),
                    LeaverType = table.Column<int>(type: "INTEGER", nullable: false),
                    ExpectationToWin = table.Column<double>(type: "REAL", precision: 5, scale: 2, nullable: false),
                    ReplayId = table.Column<int>(type: "INTEGER", nullable: false)
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
                });

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
                column: "ReplayPlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_ComboReplayRatings_RatingType",
                table: "ComboReplayRatings",
                column: "RatingType");

            migrationBuilder.CreateIndex(
                name: "IX_ComboReplayRatings_ReplayId",
                table: "ComboReplayRatings",
                column: "ReplayId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ComboPlayerRatings");

            migrationBuilder.DropTable(
                name: "ComboReplayPlayerRatings");

            migrationBuilder.DropTable(
                name: "ComboReplayRatings");
        }
    }
}
