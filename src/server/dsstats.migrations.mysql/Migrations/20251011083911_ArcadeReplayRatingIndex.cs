using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dsstats.migrations.mysql.Migrations
{
    /// <inheritdoc />
    public partial class ArcadeReplayRatingIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ArcadeReplayRatings_ArcadeReplayId",
                table: "ArcadeReplayRatings",
                column: "ArcadeReplayId");

            migrationBuilder.AddForeignKey(
                name: "FK_ArcadeReplayRatings_ArcadeReplays_ArcadeReplayId",
                table: "ArcadeReplayRatings",
                column: "ArcadeReplayId",
                principalTable: "ArcadeReplays",
                principalColumn: "ArcadeReplayId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ArcadeReplayRatings_ArcadeReplays_ArcadeReplayId",
                table: "ArcadeReplayRatings");

            migrationBuilder.DropIndex(
                name: "IX_ArcadeReplayRatings_ArcadeReplayId",
                table: "ArcadeReplayRatings");
        }
    }
}
