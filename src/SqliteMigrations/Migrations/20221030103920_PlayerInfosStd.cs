using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SqliteMigrations.Migrations
{
    public partial class PlayerInfosStd : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Wins",
                table: "Players",
                newName: "WinsStd");

            migrationBuilder.RenameColumn(
                name: "TeamGames",
                table: "Players",
                newName: "WinsCmdr");

            migrationBuilder.RenameColumn(
                name: "Mvp",
                table: "Players",
                newName: "TeamGamesStd");

            migrationBuilder.RenameColumn(
                name: "Games",
                table: "Players",
                newName: "TeamGamesCmdr");

            migrationBuilder.AddColumn<int>(
                name: "GamesCmdr",
                table: "Players",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "GamesStd",
                table: "Players",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MvpCmdr",
                table: "Players",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MvpStd",
                table: "Players",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ReplayPlayers_IsUploader_Team",
                table: "ReplayPlayers",
                columns: new[] { "IsUploader", "Team" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ReplayPlayers_IsUploader_Team",
                table: "ReplayPlayers");

            migrationBuilder.DropColumn(
                name: "GamesCmdr",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "GamesStd",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "MvpCmdr",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "MvpStd",
                table: "Players");

            migrationBuilder.RenameColumn(
                name: "WinsStd",
                table: "Players",
                newName: "Wins");

            migrationBuilder.RenameColumn(
                name: "WinsCmdr",
                table: "Players",
                newName: "TeamGames");

            migrationBuilder.RenameColumn(
                name: "TeamGamesStd",
                table: "Players",
                newName: "Mvp");

            migrationBuilder.RenameColumn(
                name: "TeamGamesCmdr",
                table: "Players",
                newName: "Games");
        }
    }
}
