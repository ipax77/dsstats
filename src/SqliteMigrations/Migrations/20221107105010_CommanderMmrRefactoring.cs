using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SqliteMigrations.Migrations
{
    public partial class CommanderMmrRefactoring : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CommanderMmrs_Commander_1_Commander_2",
                table: "CommanderMmrs");

            migrationBuilder.DropColumn(
                name: "AntiSynergyElo_1",
                table: "CommanderMmrs");

            migrationBuilder.DropColumn(
                name: "AntiSynergyElo_2",
                table: "CommanderMmrs");

            migrationBuilder.DropColumn(
                name: "AntiSynergyMmr_1",
                table: "CommanderMmrs");

            migrationBuilder.RenameColumn(
                name: "Commander_2",
                table: "CommanderMmrs",
                newName: "Race");

            migrationBuilder.RenameColumn(
                name: "Commander_1",
                table: "CommanderMmrs",
                newName: "OppRace");

            migrationBuilder.RenameColumn(
                name: "AntiSynergyMmr_2",
                table: "CommanderMmrs",
                newName: "AntiSynergyMmr");

            migrationBuilder.CreateIndex(
                name: "IX_CommanderMmrs_Race_OppRace",
                table: "CommanderMmrs",
                columns: new[] { "Race", "OppRace" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CommanderMmrs_Race_OppRace",
                table: "CommanderMmrs");

            migrationBuilder.RenameColumn(
                name: "Race",
                table: "CommanderMmrs",
                newName: "Commander_2");

            migrationBuilder.RenameColumn(
                name: "OppRace",
                table: "CommanderMmrs",
                newName: "Commander_1");

            migrationBuilder.RenameColumn(
                name: "AntiSynergyMmr",
                table: "CommanderMmrs",
                newName: "AntiSynergyMmr_2");

            migrationBuilder.AddColumn<double>(
                name: "AntiSynergyElo_1",
                table: "CommanderMmrs",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "AntiSynergyElo_2",
                table: "CommanderMmrs",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "AntiSynergyMmr_1",
                table: "CommanderMmrs",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.CreateIndex(
                name: "IX_CommanderMmrs_Commander_1_Commander_2",
                table: "CommanderMmrs",
                columns: new[] { "Commander_1", "Commander_2" });
        }
    }
}
