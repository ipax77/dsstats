using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MysqlMigrations.Migrations
{
    /// <inheritdoc />
    public partial class ArcadeDsPlayerratings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ArcadePlayerRatings_ArcadePlayers_ArcadePlayerId",
                table: "ArcadePlayerRatings");

            migrationBuilder.DropForeignKey(
                name: "FK_ArcadeReplayDsPlayers_ArcadeReplayPlayerRatings_ArcadeReplay~",
                table: "ArcadeReplayDsPlayers");

            migrationBuilder.DropForeignKey(
                name: "FK_ArcadeReplayDsPlayers_Players_PlayerId",
                table: "ArcadeReplayDsPlayers");

            migrationBuilder.DropTable(
                name: "ArcadeReplayPlayerRatings");

            migrationBuilder.DropIndex(
                name: "IX_ArcadeReplayDsPlayers_ArcadeReplayPlayerRatingId",
                table: "ArcadeReplayDsPlayers");

            migrationBuilder.DropColumn(
                name: "ArcadeReplayPlayerRatingId",
                table: "ArcadeReplayDsPlayers");

            migrationBuilder.AlterColumn<int>(
                name: "PlayerId",
                table: "ArcadeReplayDsPlayers",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ArcadePlayerId",
                table: "ArcadePlayerRatings",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "PlayerId",
                table: "ArcadePlayerRatings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ArcadeReplayDsPlayerRatings",
                columns: table => new
                {
                    ArcadeReplayDsPlayerRatingId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    GamePos = table.Column<int>(type: "int", nullable: false),
                    Rating = table.Column<float>(type: "float", nullable: false),
                    RatingChange = table.Column<float>(type: "float", nullable: false),
                    Games = table.Column<int>(type: "int", nullable: false),
                    Consistency = table.Column<float>(type: "float", nullable: false),
                    Confidence = table.Column<float>(type: "float", nullable: false),
                    ArcadeReplayDsPlayerId = table.Column<int>(type: "int", nullable: false),
                    ArcadeReplayRatingId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArcadeReplayDsPlayerRatings", x => x.ArcadeReplayDsPlayerRatingId);
                    table.ForeignKey(
                        name: "FK_ArcadeReplayDsPlayerRatings_ArcadeReplayDsPlayers_ArcadeRepl~",
                        column: x => x.ArcadeReplayDsPlayerId,
                        principalTable: "ArcadeReplayDsPlayers",
                        principalColumn: "ArcadeReplayDsPlayerId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ArcadeReplayDsPlayerRatings_ArcadeReplayRatings_ArcadeReplay~",
                        column: x => x.ArcadeReplayRatingId,
                        principalTable: "ArcadeReplayRatings",
                        principalColumn: "ArcadeReplayRatingId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ArcadePlayerRatings_PlayerId",
                table: "ArcadePlayerRatings",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_ArcadeReplayDsPlayerRatings_ArcadeReplayDsPlayerId",
                table: "ArcadeReplayDsPlayerRatings",
                column: "ArcadeReplayDsPlayerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ArcadeReplayDsPlayerRatings_ArcadeReplayRatingId",
                table: "ArcadeReplayDsPlayerRatings",
                column: "ArcadeReplayRatingId");

            migrationBuilder.AddForeignKey(
                name: "FK_ArcadePlayerRatings_ArcadePlayers_ArcadePlayerId",
                table: "ArcadePlayerRatings",
                column: "ArcadePlayerId",
                principalTable: "ArcadePlayers",
                principalColumn: "ArcadePlayerId");

            migrationBuilder.AddForeignKey(
                name: "FK_ArcadePlayerRatings_Players_PlayerId",
                table: "ArcadePlayerRatings",
                column: "PlayerId",
                principalTable: "Players",
                principalColumn: "PlayerId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ArcadeReplayDsPlayers_Players_PlayerId",
                table: "ArcadeReplayDsPlayers",
                column: "PlayerId",
                principalTable: "Players",
                principalColumn: "PlayerId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ArcadePlayerRatings_ArcadePlayers_ArcadePlayerId",
                table: "ArcadePlayerRatings");

            migrationBuilder.DropForeignKey(
                name: "FK_ArcadePlayerRatings_Players_PlayerId",
                table: "ArcadePlayerRatings");

            migrationBuilder.DropForeignKey(
                name: "FK_ArcadeReplayDsPlayers_Players_PlayerId",
                table: "ArcadeReplayDsPlayers");

            migrationBuilder.DropTable(
                name: "ArcadeReplayDsPlayerRatings");

            migrationBuilder.DropIndex(
                name: "IX_ArcadePlayerRatings_PlayerId",
                table: "ArcadePlayerRatings");

            migrationBuilder.DropColumn(
                name: "PlayerId",
                table: "ArcadePlayerRatings");

            migrationBuilder.AlterColumn<int>(
                name: "PlayerId",
                table: "ArcadeReplayDsPlayers",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "ArcadeReplayPlayerRatingId",
                table: "ArcadeReplayDsPlayers",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ArcadePlayerId",
                table: "ArcadePlayerRatings",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "ArcadeReplayPlayerRatings",
                columns: table => new
                {
                    ArcadeReplayPlayerRatingId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ArcadeReplayPlayerId = table.Column<int>(type: "int", nullable: false),
                    ArcadeReplayRatingId = table.Column<int>(type: "int", nullable: false),
                    Confidence = table.Column<float>(type: "float", nullable: false),
                    Consistency = table.Column<float>(type: "float", nullable: false),
                    GamePos = table.Column<int>(type: "int", nullable: false),
                    Games = table.Column<int>(type: "int", nullable: false),
                    Rating = table.Column<float>(type: "float", nullable: false),
                    RatingChange = table.Column<float>(type: "float", nullable: false)
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
                name: "IX_ArcadeReplayDsPlayers_ArcadeReplayPlayerRatingId",
                table: "ArcadeReplayDsPlayers",
                column: "ArcadeReplayPlayerRatingId");

            migrationBuilder.CreateIndex(
                name: "IX_ArcadeReplayPlayerRatings_ArcadeReplayPlayerId",
                table: "ArcadeReplayPlayerRatings",
                column: "ArcadeReplayPlayerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ArcadeReplayPlayerRatings_ArcadeReplayRatingId",
                table: "ArcadeReplayPlayerRatings",
                column: "ArcadeReplayRatingId");

            migrationBuilder.AddForeignKey(
                name: "FK_ArcadePlayerRatings_ArcadePlayers_ArcadePlayerId",
                table: "ArcadePlayerRatings",
                column: "ArcadePlayerId",
                principalTable: "ArcadePlayers",
                principalColumn: "ArcadePlayerId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ArcadeReplayDsPlayers_ArcadeReplayPlayerRatings_ArcadeReplay~",
                table: "ArcadeReplayDsPlayers",
                column: "ArcadeReplayPlayerRatingId",
                principalTable: "ArcadeReplayPlayerRatings",
                principalColumn: "ArcadeReplayPlayerRatingId");

            migrationBuilder.AddForeignKey(
                name: "FK_ArcadeReplayDsPlayers_Players_PlayerId",
                table: "ArcadeReplayDsPlayers",
                column: "PlayerId",
                principalTable: "Players",
                principalColumn: "PlayerId");
        }
    }
}
