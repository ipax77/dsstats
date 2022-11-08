using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MysqlMigrations.Migrations
{
    public partial class MmrChangeNullable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<float>(
                name: "MmrChange",
                table: "ReplayPlayers",
                type: "float",
                nullable: true,
                oldClrType: typeof(float),
                oldType: "float");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<float>(
                name: "MmrChange",
                table: "ReplayPlayers",
                type: "float",
                nullable: false,
                defaultValue: 0f,
                oldClrType: typeof(float),
                oldType: "float",
                oldNullable: true);
        }
    }
}
