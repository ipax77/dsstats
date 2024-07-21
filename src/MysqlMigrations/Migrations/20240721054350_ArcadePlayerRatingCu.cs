using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MysqlMigrations.Migrations
{
    /// <inheritdoc />
    public partial class ArcadePlayerRatingCu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ArcadePlayerRatings_ArcadePlayers_ArcadePlayerId",
                table: "ArcadePlayerRatings");

            migrationBuilder.DropIndex(
                name: "IX_ArcadePlayerRatings_ArcadePlayerId",
                table: "ArcadePlayerRatings");

            migrationBuilder.DropColumn(
                name: "ArcadePlayerId",
                table: "ArcadePlayerRatings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ArcadePlayerId",
                table: "ArcadePlayerRatings",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ArcadePlayerRatings_ArcadePlayerId",
                table: "ArcadePlayerRatings",
                column: "ArcadePlayerId");

            migrationBuilder.AddForeignKey(
                name: "FK_ArcadePlayerRatings_ArcadePlayers_ArcadePlayerId",
                table: "ArcadePlayerRatings",
                column: "ArcadePlayerId",
                principalTable: "ArcadePlayers",
                principalColumn: "ArcadePlayerId");
        }
    }
}
