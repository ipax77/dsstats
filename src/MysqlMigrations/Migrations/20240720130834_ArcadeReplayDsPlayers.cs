using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MysqlMigrations.Migrations
{
    /// <inheritdoc />
    public partial class ArcadeReplayDsPlayers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ArcadeReplayDsPlayers",
                columns: table => new
                {
                    ArcadeReplayDsPlayerId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SlotNumber = table.Column<int>(type: "int", nullable: false),
                    Team = table.Column<int>(type: "int", nullable: false),
                    Discriminator = table.Column<int>(type: "int", nullable: false),
                    PlayerResult = table.Column<int>(type: "int", nullable: false),
                    ArcadeReplayPlayerRatingId = table.Column<int>(type: "int", nullable: true),
                    ArcadeReplayId = table.Column<int>(type: "int", nullable: false),
                    PlayerId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArcadeReplayDsPlayers", x => x.ArcadeReplayDsPlayerId);
                    table.ForeignKey(
                        name: "FK_ArcadeReplayDsPlayers_ArcadeReplayPlayerRatings_ArcadeReplay~",
                        column: x => x.ArcadeReplayPlayerRatingId,
                        principalTable: "ArcadeReplayPlayerRatings",
                        principalColumn: "ArcadeReplayPlayerRatingId");
                    table.ForeignKey(
                        name: "FK_ArcadeReplayDsPlayers_ArcadeReplays_ArcadeReplayId",
                        column: x => x.ArcadeReplayId,
                        principalTable: "ArcadeReplays",
                        principalColumn: "ArcadeReplayId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ArcadeReplayDsPlayers_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "PlayerId");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ArcadeReplayDsPlayers_ArcadeReplayId",
                table: "ArcadeReplayDsPlayers",
                column: "ArcadeReplayId");

            migrationBuilder.CreateIndex(
                name: "IX_ArcadeReplayDsPlayers_ArcadeReplayPlayerRatingId",
                table: "ArcadeReplayDsPlayers",
                column: "ArcadeReplayPlayerRatingId");

            migrationBuilder.CreateIndex(
                name: "IX_ArcadeReplayDsPlayers_PlayerId",
                table: "ArcadeReplayDsPlayers",
                column: "PlayerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ArcadeReplayDsPlayers");
        }
    }
}
