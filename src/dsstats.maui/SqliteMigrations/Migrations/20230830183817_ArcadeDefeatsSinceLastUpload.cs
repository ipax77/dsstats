using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SqliteMigrations.Migrations
{
    public partial class ArcadeDefeatsSinceLastUpload : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ArcadeDefeatsSinceLastUpload",
                table: "Players",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ArcadeDefeatsSinceLastUpload",
                table: "PlayerRatings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "DamageEnts",
                columns: table => new
                {
                    Commander = table.Column<int>(type: "INTEGER", nullable: false),
                    Breakpoint = table.Column<int>(type: "INTEGER", nullable: false),
                    Count = table.Column<int>(type: "INTEGER", nullable: false),
                    Mvp = table.Column<int>(type: "INTEGER", nullable: false),
                    AvgKills = table.Column<int>(type: "INTEGER", nullable: false),
                    AvgArmy = table.Column<int>(type: "INTEGER", nullable: false),
                    AvgUpgrades = table.Column<int>(type: "INTEGER", nullable: false),
                    AvgGas = table.Column<double>(type: "REAL", nullable: false),
                    AvgIncome = table.Column<int>(type: "INTEGER", nullable: false),
                    AvgAPM = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "SynergyEnts",
                columns: table => new
                {
                    Commander = table.Column<int>(type: "INTEGER", nullable: false),
                    Teammate = table.Column<int>(type: "INTEGER", nullable: false),
                    Count = table.Column<int>(type: "INTEGER", nullable: false),
                    Wins = table.Column<int>(type: "INTEGER", nullable: false),
                    AvgRating = table.Column<double>(type: "REAL", nullable: false),
                    AvgGain = table.Column<double>(type: "REAL", nullable: false),
                    NormalizedAvgGain = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "WinrateEnts",
                columns: table => new
                {
                    Commander = table.Column<int>(type: "INTEGER", nullable: false),
                    Count = table.Column<int>(type: "INTEGER", nullable: false),
                    Wins = table.Column<int>(type: "INTEGER", nullable: false),
                    AvgRating = table.Column<double>(type: "REAL", nullable: false),
                    AvgGain = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DamageEnts");

            migrationBuilder.DropTable(
                name: "SynergyEnts");

            migrationBuilder.DropTable(
                name: "WinrateEnts");

            migrationBuilder.DropColumn(
                name: "ArcadeDefeatsSinceLastUpload",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "ArcadeDefeatsSinceLastUpload",
                table: "PlayerRatings");
        }
    }
}
