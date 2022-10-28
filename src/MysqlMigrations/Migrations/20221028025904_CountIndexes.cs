using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MysqlMigrations.Migrations
{
    public partial class CountIndexes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Replays_GameTime_GameMode_Maxleaver",
                table: "Replays",
                columns: new[] { "GameTime", "GameMode", "Maxleaver" });

            migrationBuilder.CreateIndex(
                name: "IX_Replays_GameTime_GameMode_WinnerTeam",
                table: "Replays",
                columns: new[] { "GameTime", "GameMode", "WinnerTeam" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Replays_GameTime_GameMode_Maxleaver",
                table: "Replays");

            migrationBuilder.DropIndex(
                name: "IX_Replays_GameTime_GameMode_WinnerTeam",
                table: "Replays");
        }
    }
}
