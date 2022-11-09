using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MysqlMigrations.Migrations
{
    public partial class UnitWithoutCmdr : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Units_Name_Commander",
                table: "Units");

            migrationBuilder.DropColumn(
                name: "Commander",
                table: "Units");

            migrationBuilder.DropColumn(
                name: "Cost",
                table: "Units");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Commander",
                table: "Units",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Cost",
                table: "Units",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Units_Name_Commander",
                table: "Units",
                columns: new[] { "Name", "Commander" },
                unique: true);
        }
    }
}
