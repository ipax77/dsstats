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
                columns: table => new {
                    CommanderMmrId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Commander = table.Column<int>(type: "INTEGER", nullable: false),
                    SynCommander = table.Column<int>(type: "INTEGER", nullable: false),
                    Synergy = table.Column<double>(type: "REAL", nullable: false),
                    AntiSynergy = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table => {
                    table.PrimaryKey("PK_CommanderMmrs", x => x.CommanderMmrId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CommanderMmrs_Commander_SynCommander",
                table: "CommanderMmrs",
                columns: new[] { "Commander", "SynCommander" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CommanderMmrs");
        }
    }
}
