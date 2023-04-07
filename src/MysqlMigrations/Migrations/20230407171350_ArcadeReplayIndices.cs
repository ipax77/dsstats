using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MysqlMigrations.Migrations
{
    public partial class ArcadeReplayIndices : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ArcadeReplays_Id",
                table: "ArcadeReplays",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_ArcadeReplays_RegionId_GameMode_CreatedAt",
                table: "ArcadeReplays",
                columns: new[] { "RegionId", "GameMode", "CreatedAt" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ArcadeReplays_Id",
                table: "ArcadeReplays");

            migrationBuilder.DropIndex(
                name: "IX_ArcadeReplays_RegionId_GameMode_CreatedAt",
                table: "ArcadeReplays");
        }
    }
}
