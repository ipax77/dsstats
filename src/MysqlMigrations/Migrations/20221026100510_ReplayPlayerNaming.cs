using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MysqlMigrations.Migrations
{
    public partial class ReplayPlayerNaming : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Spawns_ReplayPlayers_ReplayPlayerId",
                table: "Spawns");

            migrationBuilder.AlterColumn<int>(
                name: "ReplayPlayerId",
                table: "Spawns",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Spawns_ReplayPlayers_ReplayPlayerId",
                table: "Spawns",
                column: "ReplayPlayerId",
                principalTable: "ReplayPlayers",
                principalColumn: "ReplayPlayerId",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Spawns_ReplayPlayers_ReplayPlayerId",
                table: "Spawns");

            migrationBuilder.AlterColumn<int>(
                name: "ReplayPlayerId",
                table: "Spawns",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_Spawns_ReplayPlayers_ReplayPlayerId",
                table: "Spawns",
                column: "ReplayPlayerId",
                principalTable: "ReplayPlayers",
                principalColumn: "ReplayPlayerId");
        }
    }
}
