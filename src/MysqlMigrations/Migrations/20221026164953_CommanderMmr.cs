using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MysqlMigrations.Migrations
{
    public partial class CommanderMmr : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CommanderMmrs",
                columns: table => new
                {
                    CommanderMmrId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Commander = table.Column<int>(type: "int", nullable: false),
                    SynCommander = table.Column<int>(type: "int", nullable: false),
                    Synergy = table.Column<double>(type: "double", nullable: false),
                    AntiSynergy = table.Column<double>(type: "double", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommanderMmrs", x => x.CommanderMmrId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

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
