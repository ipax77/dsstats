using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SqliteMigrations.Migrations
{
    public partial class CheatDetectValues : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GamesCmdr",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "GamesStd",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "LeaverCount",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "MainCommander",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "MainCount",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "Mmr",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "MmrOverTime",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "MmrStd",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "MmrStdOverTime",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "MvpCmdr",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "MvpStd",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "TeamGamesCmdr",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "TeamGamesStd",
                table: "Players");

            migrationBuilder.RenameColumn(
                name: "WinsStd",
                table: "Players",
                newName: "RageQuitCount");

            migrationBuilder.RenameColumn(
                name: "WinsCmdr",
                table: "Players",
                newName: "DisconnectCount");

            migrationBuilder.AddColumn<bool>(
                name: "ResultCorrected",
                table: "Replays",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Units_Name",
                table: "Units",
                column: "Name",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Units_Name",
                table: "Units");

            migrationBuilder.DropColumn(
                name: "ResultCorrected",
                table: "Replays");

            migrationBuilder.RenameColumn(
                name: "RageQuitCount",
                table: "Players",
                newName: "WinsStd");

            migrationBuilder.RenameColumn(
                name: "DisconnectCount",
                table: "Players",
                newName: "WinsCmdr");

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
                name: "LeaverCount",
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

            migrationBuilder.AddColumn<double>(
                name: "Mmr",
                table: "Players",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "MmrOverTime",
                table: "Players",
                type: "TEXT",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "MmrStd",
                table: "Players",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "MmrStdOverTime",
                table: "Players",
                type: "TEXT",
                maxLength: 2000,
                nullable: true);

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

            migrationBuilder.AddColumn<int>(
                name: "TeamGamesCmdr",
                table: "Players",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TeamGamesStd",
                table: "Players",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }
    }
}
