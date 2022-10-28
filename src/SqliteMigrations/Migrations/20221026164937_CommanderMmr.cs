using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SqliteMigrations.Migrations
{
    public partial class CommanderMmr : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CommanderMmrs",
                columns: table => new
                {
                    CommanderMmrId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Commander_1 = table.Column<int>(type: "INTEGER", nullable: false),
                    Commander_2 = table.Column<int>(type: "INTEGER", nullable: false),
                    AntiSynergyMmr_1 = table.Column<double>(type: "REAL", nullable: false),
                    AntiSynergyMmr_2 = table.Column<double>(type: "REAL", nullable: false),
                    AntiSynergyElo_1 = table.Column<double>(type: "REAL", nullable: false),
                    AntiSynergyElo_2 = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommanderMmrs", x => x.CommanderMmrId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CommanderMmrs_Commander_1_Commander_2",
                table: "CommanderMmrs",
                columns: new[] { "Commander_1", "Commander_2" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CommanderMmrs");
        }
    }
}
