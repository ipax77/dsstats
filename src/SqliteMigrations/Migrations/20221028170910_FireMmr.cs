using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SqliteMigrations.Migrations
{
    public partial class FireMmr : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Replays_GameTime_GameMode_Maxleaver",
                table: "Replays");

            migrationBuilder.DropIndex(
                name: "IX_Replays_GameTime_GameMode_WinnerTeam",
                table: "Replays");

            migrationBuilder.RenameColumn(
                name: "Synergy",
                table: "CommanderMmrs",
                newName: "SynergyMmr");

            migrationBuilder.RenameColumn(
                name: "SynCommander",
                table: "CommanderMmrs",
                newName: "Commander_2");

            migrationBuilder.RenameColumn(
                name: "Commander",
                table: "CommanderMmrs",
                newName: "Commander_1");

            migrationBuilder.RenameColumn(
                name: "AntiSynergy",
                table: "CommanderMmrs",
                newName: "AntiSynergyMmr_2");

            migrationBuilder.RenameIndex(
                name: "IX_CommanderMmrs_Commander_SynCommander",
                table: "CommanderMmrs",
                newName: "IX_CommanderMmrs_Commander_1_Commander_2");

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
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
                name: "SynergyMmr",
                table: "CommanderMmrs",
                newName: "Synergy");

            migrationBuilder.RenameColumn(
                name: "Commander_2",
                table: "CommanderMmrs",
                newName: "SynCommander");

            migrationBuilder.RenameColumn(
                name: "Commander_1",
                table: "CommanderMmrs",
                newName: "Commander");

            migrationBuilder.RenameColumn(
                name: "AntiSynergyMmr_2",
                table: "CommanderMmrs",
                newName: "AntiSynergy");

            migrationBuilder.RenameIndex(
                name: "IX_CommanderMmrs_Commander_1_Commander_2",
                table: "CommanderMmrs",
                newName: "IX_CommanderMmrs_Commander_SynCommander");

            migrationBuilder.CreateIndex(
                name: "IX_Replays_GameTime_GameMode_Maxleaver",
                table: "Replays",
                columns: new[] { "GameTime", "GameMode", "Maxleaver" });

            migrationBuilder.CreateIndex(
                name: "IX_Replays_GameTime_GameMode_WinnerTeam",
                table: "Replays",
                columns: new[] { "GameTime", "GameMode", "WinnerTeam" });
        }
    }
}
