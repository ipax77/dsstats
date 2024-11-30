using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MysqlMigrations.Migrations
{
    /// <inheritdoc />
    public partial class ReplayArcadeMatch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReplayArcadeMatches",
                columns: table => new
                {
                    ReplayArcadeMatchId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ReplayId = table.Column<int>(type: "int", nullable: false),
                    ArcadeReplayId = table.Column<int>(type: "int", nullable: false),
                    MatchTime = table.Column<DateTime>(type: "datetime(0)", precision: 0, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReplayArcadeMatches", x => x.ReplayArcadeMatchId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ReplayArcadeMatches_ArcadeReplayId",
                table: "ReplayArcadeMatches",
                column: "ArcadeReplayId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReplayArcadeMatches_MatchTime",
                table: "ReplayArcadeMatches",
                column: "MatchTime");

            migrationBuilder.CreateIndex(
                name: "IX_ReplayArcadeMatches_ReplayId",
                table: "ReplayArcadeMatches",
                column: "ReplayId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReplayArcadeMatches");
        }
    }
}
