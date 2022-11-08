using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SqliteMigrations.Migrations
{
    public partial class MmrChangeNullable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<float>(
                name: "MmrChange",
                table: "ReplayPlayers",
                type: "REAL",
                nullable: true,
                oldClrType: typeof(float),
                oldType: "REAL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<float>(
                name: "MmrChange",
                table: "ReplayPlayers",
                type: "REAL",
                nullable: false,
                defaultValue: 0f,
                oldClrType: typeof(float),
                oldType: "REAL",
                oldNullable: true);
        }
    }
}
