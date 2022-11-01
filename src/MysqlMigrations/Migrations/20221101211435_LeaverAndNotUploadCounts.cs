using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MysqlMigrations.Migrations
{
    public partial class LeaverAndNotUploadCounts : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "DidNotUpload",
                table: "ReplayPlayers",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsLeaver",
                table: "ReplayPlayers",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "LeaverCount",
                table: "Players",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "NotUploadCount",
                table: "Players",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DidNotUpload",
                table: "ReplayPlayers");

            migrationBuilder.DropColumn(
                name: "IsLeaver",
                table: "ReplayPlayers");

            migrationBuilder.DropColumn(
                name: "LeaverCount",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "NotUploadCount",
                table: "Players");
        }
    }
}
