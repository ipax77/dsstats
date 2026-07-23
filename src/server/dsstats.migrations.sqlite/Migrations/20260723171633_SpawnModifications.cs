using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dsstats.migrations.sqlite.Migrations
{
    /// <inheritdoc />
    public partial class SpawnModifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ScanCount",
                table: "ReplayPlayers",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SpawnModifications",
                columns: table => new
                {
                    SpawnModificationId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Count = table.Column<int>(type: "INTEGER", nullable: false),
                    UnitId = table.Column<int>(type: "INTEGER", nullable: false),
                    SpawnId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpawnModifications", x => x.SpawnModificationId);
                    table.ForeignKey(
                        name: "FK_SpawnModifications_Spawns_SpawnId",
                        column: x => x.SpawnId,
                        principalTable: "Spawns",
                        principalColumn: "SpawnId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SpawnModifications_Units_UnitId",
                        column: x => x.UnitId,
                        principalTable: "Units",
                        principalColumn: "UnitId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SpawnModifications_SpawnId",
                table: "SpawnModifications",
                column: "SpawnId");

            migrationBuilder.CreateIndex(
                name: "IX_SpawnModifications_UnitId",
                table: "SpawnModifications",
                column: "UnitId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SpawnModifications");

            migrationBuilder.DropColumn(
                name: "ScanCount",
                table: "ReplayPlayers");
        }
    }
}
