using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SqliteMigrations.Migrations
{
    public partial class PlayerMmrStd : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DsROverTime",
                table: "Players",
                newName: "MmrOverTime");

            migrationBuilder.RenameColumn(
                name: "DsR",
                table: "Players",
                newName: "MmrStd");

            migrationBuilder.AddColumn<double>(
                name: "Mmr",
                table: "Players",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Mmr",
                table: "Players");

            migrationBuilder.RenameColumn(
                name: "MmrStd",
                table: "Players",
                newName: "DsR");

            migrationBuilder.RenameColumn(
                name: "MmrOverTime",
                table: "Players",
                newName: "DsROverTime");
        }
    }
}
