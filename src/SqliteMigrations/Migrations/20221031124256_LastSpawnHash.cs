using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SqliteMigrations.Migrations
{
    public partial class LastSpawnHash : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LastSpawnHash",
                table: "ReplayPlayers",
                type: "TEXT",
                fixedLength: true,
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReplayPlayers_LastSpawnHash",
                table: "ReplayPlayers",
                column: "LastSpawnHash",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ReplayPlayers_LastSpawnHash",
                table: "ReplayPlayers");

            migrationBuilder.DropColumn(
                name: "LastSpawnHash",
                table: "ReplayPlayers");
        }
    }
}
