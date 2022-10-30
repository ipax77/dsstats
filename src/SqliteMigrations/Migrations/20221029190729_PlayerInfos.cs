using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SqliteMigrations.Migrations
{
    public partial class PlayerInfos : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Games",
                table: "Uploaders",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MainCommander",
                table: "Uploaders",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MainCount",
                table: "Uploaders",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Mvp",
                table: "Uploaders",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TeamGames",
                table: "Uploaders",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Wins",
                table: "Uploaders",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Games",
                table: "Players",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MainCommander",
                table: "Players",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MainCount",
                table: "Players",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Mvp",
                table: "Players",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TeamGames",
                table: "Players",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Wins",
                table: "Players",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Replays_Maxkillsum",
                table: "Replays",
                column: "Maxkillsum");

            migrationBuilder.CreateIndex(
                name: "IX_ReplayPlayers_Kills",
                table: "ReplayPlayers",
                column: "Kills");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Replays_Maxkillsum",
                table: "Replays");

            migrationBuilder.DropIndex(
                name: "IX_ReplayPlayers_Kills",
                table: "ReplayPlayers");

            migrationBuilder.DropColumn(
                name: "Games",
                table: "Uploaders");

            migrationBuilder.DropColumn(
                name: "MainCommander",
                table: "Uploaders");

            migrationBuilder.DropColumn(
                name: "MainCount",
                table: "Uploaders");

            migrationBuilder.DropColumn(
                name: "Mvp",
                table: "Uploaders");

            migrationBuilder.DropColumn(
                name: "TeamGames",
                table: "Uploaders");

            migrationBuilder.DropColumn(
                name: "Wins",
                table: "Uploaders");

            migrationBuilder.DropColumn(
                name: "Games",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "MainCommander",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "MainCount",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "Mvp",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "TeamGames",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "Wins",
                table: "Players");
        }
    }
}
