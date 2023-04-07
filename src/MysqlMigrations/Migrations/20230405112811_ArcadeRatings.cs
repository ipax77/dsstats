using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MysqlMigrations.Migrations
{
    public partial class ArcadeRatings : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                    MmrOverTime = table.Column<string>(type: "varchar(4000)", maxLength: 4000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
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
                name: "IX_ArcadeReplayPlayerRatings_ArcadeReplayPlayerId",
                table: "ArcadeReplayPlayerRatings",
                column: "ArcadeReplayPlayerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ArcadeReplayPlayerRatings_ArcadeReplayRatingId",
                table: "ArcadeReplayPlayerRatings",
                column: "ArcadeReplayRatingId");

            migrationBuilder.CreateIndex(
                name: "IX_ArcadeReplayRatings_ArcadeReplayId",
                table: "ArcadeReplayRatings",
                column: "ArcadeReplayId",
                unique: true);
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
                name: "ArcadeReplayRatings");
        }
    }
}
