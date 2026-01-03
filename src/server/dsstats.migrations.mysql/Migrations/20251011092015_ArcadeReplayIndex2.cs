using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dsstats.migrations.mysql.Migrations
{
    /// <inheritdoc />
    public partial class ArcadeReplayIndex2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ArcadeReplays_CreatedAt_ArcadeReplayId",
                table: "ArcadeReplays");

            migrationBuilder.CreateIndex(
                name: "IX_ArcadeReplays_CreatedAt_ArcadeReplayId",
                table: "ArcadeReplays",
                columns: new[] { "CreatedAt", "ArcadeReplayId" },
                descending: new[] { false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ArcadeReplays_CreatedAt_ArcadeReplayId",
                table: "ArcadeReplays");

            migrationBuilder.CreateIndex(
                name: "IX_ArcadeReplays_CreatedAt_ArcadeReplayId",
                table: "ArcadeReplays",
                columns: new[] { "CreatedAt", "ArcadeReplayId" });
        }
    }
}
