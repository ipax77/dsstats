using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MysqlMigrations.Migrations
{
    public partial class PlayerRatingsPos : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReplayPlayerRatings",
                columns: table => new
                {
                    ReplayPlayerRatingId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    MmrChange = table.Column<double>(type: "double", nullable: false),
                    Pos = table.Column<int>(type: "int", nullable: false),
                    ReplayPlayerId = table.Column<int>(type: "int", nullable: false),
                    ReplayId = table.Column<int>(type: "int", nullable: false)
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
                    table.ForeignKey(
                        name: "FK_ReplayPlayerRatings_Replays_ReplayId",
                        column: x => x.ReplayId,
                        principalTable: "Replays",
                        principalColumn: "ReplayId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ReplayPlayerRatings_ReplayId",
                table: "ReplayPlayerRatings",
                column: "ReplayId");

            migrationBuilder.CreateIndex(
                name: "IX_ReplayPlayerRatings_ReplayPlayerId",
                table: "ReplayPlayerRatings",
                column: "ReplayPlayerId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReplayPlayerRatings");
        }
    }
}
