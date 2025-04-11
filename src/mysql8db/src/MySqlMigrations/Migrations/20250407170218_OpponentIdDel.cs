using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MySqlMigrations.Migrations
{
    /// <inheritdoc />
    public partial class OpponentIdDel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReplayPlayers_ReplayPlayers_OpponentId",
                table: "ReplayPlayers");

            migrationBuilder.AddForeignKey(
                name: "FK_ReplayPlayers_ReplayPlayers_OpponentId",
                table: "ReplayPlayers",
                column: "OpponentId",
                principalTable: "ReplayPlayers",
                principalColumn: "ReplayPlayerId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReplayPlayers_ReplayPlayers_OpponentId",
                table: "ReplayPlayers");

            migrationBuilder.AddForeignKey(
                name: "FK_ReplayPlayers_ReplayPlayers_OpponentId",
                table: "ReplayPlayers",
                column: "OpponentId",
                principalTable: "ReplayPlayers",
                principalColumn: "ReplayPlayerId");
        }
    }
}
