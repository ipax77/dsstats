using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dsstats.migrations.mysql.Migrations
{
    /// <inheritdoc />
    public partial class ArcadeReplayIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ArcadeReplays_CreatedAt",
                table: "ArcadeReplays");

            migrationBuilder.CreateIndex(
                name: "IX_ArcadeReplays_CreatedAt_ArcadeReplayId",
                table: "ArcadeReplays",
                columns: new[] { "CreatedAt", "ArcadeReplayId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ArcadeReplays_CreatedAt_ArcadeReplayId",
                table: "ArcadeReplays");

            migrationBuilder.CreateIndex(
                name: "IX_ArcadeReplays_CreatedAt",
                table: "ArcadeReplays",
                column: "CreatedAt");
        }
    }
}
