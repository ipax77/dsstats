using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MysqlMigrations.Migrations
{
    public partial class ComboPreRating : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ComboReplayRatings_ReplayId",
                table: "ComboReplayRatings");

            migrationBuilder.DropIndex(
                name: "IX_ComboReplayPlayerRatings_ReplayPlayerId",
                table: "ComboReplayPlayerRatings");

            migrationBuilder.AddColumn<bool>(
                name: "IsPreRating",
                table: "ComboReplayRatings",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_ComboReplayRatings_ReplayId",
                table: "ComboReplayRatings",
                column: "ReplayId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ComboReplayPlayerRatings_ReplayPlayerId",
                table: "ComboReplayPlayerRatings",
                column: "ReplayPlayerId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ComboReplayRatings_ReplayId",
                table: "ComboReplayRatings");

            migrationBuilder.DropIndex(
                name: "IX_ComboReplayPlayerRatings_ReplayPlayerId",
                table: "ComboReplayPlayerRatings");

            migrationBuilder.DropColumn(
                name: "IsPreRating",
                table: "ComboReplayRatings");

            migrationBuilder.CreateIndex(
                name: "IX_ComboReplayRatings_ReplayId",
                table: "ComboReplayRatings",
                column: "ReplayId");

            migrationBuilder.CreateIndex(
                name: "IX_ComboReplayPlayerRatings_ReplayPlayerId",
                table: "ComboReplayPlayerRatings",
                column: "ReplayPlayerId");
        }
    }
}
